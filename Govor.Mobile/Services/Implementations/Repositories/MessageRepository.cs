using AutoMapper;
using Govor.Mobile.Data;
using Govor.Mobile.Models.Requests;
using Govor.Mobile.Models.Responses;
using Govor.Mobile.Services.Api;
using Govor.Mobile.Services.Hubs;
using Govor.Mobile.Services.Interfaces.Profiles;
using Govor.Mobile.Services.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Govor.Mobile.Services.Implementations.Repositories;

public class MessagesRepository : IMessagesRepository
{
    private readonly IDbContextFactory<GovorDbContext> _contextFactory;
    private readonly IChatLoaderApi _api;
    private readonly IChatHub _hub;
    private readonly IMapper _mapper;
    private readonly IUserProfileService _profileService;
    private readonly IMemoryCache _cache;
    private readonly MemoryCacheEntryOptions _cacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
        Size = 1,
        Priority = CacheItemPriority.High
    };
    
    private bool _isInitialized = false;
    private Guid? _cachedCurrentUserId;
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    public async Task ImportLegacyChatsAsync(IEnumerable<Guid> authorizedChatIds)
    {
        var path = Path.Combine(FileSystem.AppDataDirectory, "govor.db");
        var chats = authorizedChatIds.Where(id => id != Guid.Empty).Distinct().ToArray();
        if (!File.Exists(path) || chats.Length == 0) return;
        try
        {
            // Import only chats that the server lists for the current account; retain the old file.
            await using var legacy = new GovorDbContext(new DbContextOptionsBuilder<GovorDbContext>()
                .UseSqlite($"Data Source={path};Mode=ReadOnly").Options);
            var messages = await legacy.Messages.AsNoTracking().Where(m => chats.Contains(m.ChatId)).ToListAsync();
            foreach (var group in messages.GroupBy(m => m.ChatId))
                await SaveBatchAsync(_mapper.Map<List<MessageResponse>>(group.ToList()), group.Key, notify: false);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Legacy message cache import failed: {ex.Message}");
        }
    }
    
    public event Action<MessageResponse>? OnNewMessage;
    public event Action<MessageResponse>? OnMessageUpdated;
    public event Action<MessageView>? OnMessageViewed;
    public event Action<Guid>? OnMessageDeleted;
    
    public MessagesRepository(
        IDbContextFactory<GovorDbContext> contextFactory,
        IChatLoaderApi api,
        IChatHub hub,
        IMapper mapper,
        IMemoryCache cache,
        IUserProfileService profileService)
    {
        _contextFactory = contextFactory;
        _api = api;
        _hub = hub;
        _mapper = mapper;
        _cache = cache;
        _profileService = profileService;
    }

    private async Task<Guid> GetMyIdAsync()
    {
        if (_cachedCurrentUserId.HasValue) return _cachedCurrentUserId.Value;
        var profile = await _profileService.GetCurrentProfileAsync();
        _cachedCurrentUserId = profile.Id;
        return profile.Id;
    }

    public void Initialize()
    {
        if (_isInitialized) return;

        // При событии MessageSent (подтверждение отправки) мы просто сохраняем сообщение как новое входящее
        _hub.ReceiveMessage += async (msg) => await SafeExecute(() => SaveOrUpdateMessageAsync(msg));
        _hub.MessageSent += async (msg) => await SafeExecute(() => SaveOrUpdateMessageAsync(msg));
        _hub.MessageRead += async (dto) => await SafeExecute(() => UpdateMessageViewersAsync(dto));
        _hub.MessageEdited += async (msg) => await SafeExecute(() => UpdateMessageContentAsync(msg));
        _hub.MessageRemoved += async (msg) => await SafeExecute(() => DeleteMessageAsync(msg.MessageId));

        _isInitialized = true;
    }

    private async Task UpdateMessageViewersAsync(MessageReadResponse dto)
    {
        await _writeLock.WaitAsync();
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var entity = await context.Messages.FirstOrDefaultAsync(m => m.Id == dto.MessageId);

            if (entity is null)
                return;

            var isNewViewer = entity.MessageViews.All(mv => mv.UserId != dto.ReaderId);
       
            MessageView view = new MessageView
            {
                Id = dto.ViewId,
                ChatId = entity.ChatId,
                RecipientType = entity.RecipientType,
                MessageId = dto.MessageId,
                UserId = dto.ReaderId,
                ViewedAt = dto.WhenWas,
            };

            if (isNewViewer)
            {
                entity.MessageViews.Add(view);
                await context.SaveChangesAsync();

                OnMessageViewed?.Invoke(view);
            }
        }
        finally { _writeLock.Release(); }
    }

    public async Task<List<MessageResponse>> GetMessagesLocalAsync(Guid chatId, int count = 50, bool group = false, Guid startMessage = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        var entities = await context.Messages
            .AsNoTracking()
            .Where(x => x.ChatId == chatId && (
                group == false ? 
                    (x.RecipientType == RecipientType.User) : (x.RecipientType == RecipientType.Group))
            )
            .OrderByDescending(x => x.SentAt)
            .Take(count)
            .ToListAsync();
        
        return _mapper.Map<List<MessageResponse>>(entities);
    }

    // ЛОГИКА ИЗМЕНЕНА ЗДЕСЬ
    public async Task SendMessageAsync(MessageRequest request)
    {
        // Мы просто отправляем запрос в сокет.
        // Если отправка успешна, сервер пришлет событие MessageSent, 
        // которое поймает Initialize и сохранит сообщение в БД.
        
        var result = await _hub.Send(request);
        
        if (result.Status != HubResultStatus.Success)
        {
            // Здесь можно выбросить исключение, чтобы UI показал ошибку (Toast/Alert),
            // так как локального сообщения "Error" мы больше не создаем.
            throw new InvalidOperationException(result.ErrorMessage ?? "Не удалось отправить сообщение");
        }
        if (result.Value is null || result.Value.MessageId == Guid.Empty)
            throw new InvalidDataException("Сервер не подтвердил сохранение сообщения.");
        await SaveOrUpdateMessageAsync(result.Value);
    }

    public async Task<bool> ReadMessageAsync(Guid messageId)
    {
        var myId = await GetMyIdAsync();
        await using (var context = await _contextFactory.CreateDbContextAsync())
        {
            var message = await context.Messages.AsNoTracking().FirstOrDefaultAsync(m => m.Id == messageId);
            if (message is null || message.SenderId == myId) return false;
            if (message.MessageViews.Any(v => v.UserId == myId)) return true;
        }
        var request = new ReadMessageRequest { MessageId = messageId };

        var result = await _hub.Read(request);

        if (result.Status != HubResultStatus.Success || result.Value is null) return false;
        await UpdateMessageViewersAsync(result.Value);
        return true;
    }

    public async Task<bool> EditMessageAsync(Guid messageId, string newText)
    {
        var request = new EditMessageRequest {
            MessageId = messageId,
            NewEncryptedContent = newText 
        };

        var result = await _hub.Edit(request);

        if (result.Status != HubResultStatus.Success)
        {
            // Здесь можно выбросить исключение, чтобы UI показал ошибку (Toast/Alert),
            // так как локального сообщения "Error" мы больше не создаем.
            //throw new Exception(result.ErrorMessage ?? "Не удалось удалить сообщение");
        }
        
        return result.Status == HubResultStatus.Success;
    }

    public async Task RemoveMessageAsync(Guid messageId, bool forceRemove = true)
    {
        var request = new RemoveMessageRequest { 
            MessageId = messageId,
            RequestType = forceRemove ?
                RemoveMessageRequestType.ForceRemove : RemoveMessageRequestType.HideForMe 
        };

        var result = await _hub.Remove(request);

        if (result.Status != HubResultStatus.Success)
        {
            // Здесь можно выбросить исключение, чтобы UI показал ошибку (Toast/Alert),
            // так как локального сообщения "Error" мы больше не создаем.
            //throw new Exception(result.ErrorMessage ?? "Не удалось удалить сообщение");
        }
    }

    public async Task SyncChatAsync(Guid chatId, bool group = false, int after = 50)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        var lastMsg = await context.Messages
            .AsNoTracking()
            .Where(x => x.ChatId == chatId)
            .OrderByDescending(x => x.SentAt)
            .FirstOrDefaultAsync();

        after = Math.Clamp(after, 1, 100);
        var query = new MessageQuery
        {
            StartMessageId = lastMsg?.Id,
            Before = lastMsg is null ? after : 0,
            After = lastMsg is null ? 0 : after
        };

        var remoteResult = group 
            ? await _api.GetGroupMessages(chatId, query) 
            : await _api.GetUserMessages(chatId, query);

        if (remoteResult.IsSuccess && remoteResult.Value?.Any() == true)
        {
            await SaveBatchAsync(remoteResult.Value, chatId);
        }
    }

    public async Task<List<MessageResponse>> LoadHistoryAsync(
        Guid chatId,
        Guid? oldestMessageId,
        int before = 50,
        bool group = false)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        DateTime? oldestSentAt = null;

        if (oldestMessageId.HasValue)
            oldestSentAt = await context.Messages
                .Where(x => x.Id == oldestMessageId.Value)
                .Select(x => x.SentAt)
                .FirstOrDefaultAsync();

        // ----------------------------
        // 1️⃣ Попытка загрузить из БД
        // ----------------------------

        var localQuery = context.Messages
            .AsNoTracking()
            .Where(x => x.ChatId == chatId);

        localQuery = group
            ? localQuery.Where(x => x.RecipientType == RecipientType.Group)
            : localQuery.Where(x => x.RecipientType == RecipientType.User);

        if (oldestSentAt.HasValue)
            localQuery = localQuery.Where(x => x.SentAt < oldestSentAt.Value);

        var localEntities = await localQuery
            .OrderByDescending(x => x.SentAt)
            .Take(before)
            .ToListAsync();

        // Если достаточно локальных данных — возвращаем их
        if (localEntities.Count == before) return _mapper.Map<List<MessageResponse>>(localEntities);

        // ----------------------------
        // 2️⃣ Запрос на сервер
        // ----------------------------

        var query = new MessageQuery
        {
            StartMessageId = oldestMessageId,
            Before = Math.Clamp(before, 1, 100),
            After = 0
        };

        var result = group
            ? await _api.GetGroupMessages(chatId, query)
            : await _api.GetUserMessages(chatId, query);

        if (!result.IsSuccess || result.Value == null || !result.Value.Any())
            return _mapper.Map<List<MessageResponse>>(localEntities);

        // ----------------------------
        // 3️⃣ Сохраняем новые сообщения
        // ----------------------------

        await SaveBatchAsync(result.Value, chatId, notify: false);

        // ----------------------------
        // 4️⃣ Объединяем и удаляем дубли
        // ----------------------------

        var merged = localEntities
            .Select(x => _mapper.Map<MessageResponse>(x))
            .Concat(result.Value)
            .Where(x => x.Id != oldestMessageId)
            .GroupBy(x => x.Id)
            .Select(g => g.First())
            .OrderByDescending(x => x.SentAt)
            .Take(before)
            .ToList();

        return merged;
    }

    private async Task SaveBatchAsync(List<MessageResponse> messages, Guid chatId, bool notify = true)
    {
        await _writeLock.WaitAsync();
        try { await SaveBatchCoreAsync(messages, chatId, notify); }
        finally { _writeLock.Release(); }
    }

    private async Task SaveBatchCoreAsync(List<MessageResponse> messages, Guid chatId, bool notify)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var newMessages = new List<LocalMessage>();
        
        foreach (var msg in messages.DistinctBy(x => x.Id))
        {
            // Избегаем дубликатов при пакетной вставке
            if (await context.Messages.AnyAsync(x => x.Id == msg.Id)) continue;

            var entity = _mapper.Map<LocalMessage>(msg);
            entity.ChatId = chatId;
            entity.LocalStatus = 0; // 0 = Synced
            
            await context.Messages.AddAsync(entity);
            newMessages.Add(entity);
        }
        
        newMessages = newMessages.OrderBy(x => x.SentAt).ToList();
        
        if (newMessages.Any())
        {
            await context.SaveChangesAsync();
            
            if(!notify)
                return;
            
            foreach (var entity in newMessages)
            {
                NotifyNewMessage(entity);
            }
        }
    }

    // Единый метод для сохранения входящего или отправленного (подтвержденного) сообщения
    private async Task SaveOrUpdateMessageAsync(UserMessageResponse msg)
    {
        await _writeLock.WaitAsync();
        try { await SaveOrUpdateMessageCoreAsync(msg); }
        finally { _writeLock.Release(); }
    }

    private async Task SaveOrUpdateMessageCoreAsync(UserMessageResponse msg)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        /*
        Guid chatId = (msg.RecipientType == RecipientType.Group) 
            ? msg.RecipientId 
            : (msg.SenderId == myId ? msg.RecipientId : msg.SenderId); */
       
        Guid chatId = msg.RecipientId;
        
        var existing = await context.Messages.FindAsync(msg.MessageId);
        
        if (existing != null)
        {
            // Если сообщение уже есть (например, пришло и REST и Socket одновременно), обновляем его
            _mapper.Map(msg, existing);
            existing.LocalStatus = 0;
            await context.SaveChangesAsync();
            NotifyMessageUpdated(existing);
        }
        else
        {
            // Сообщения нет - создаем новое
            var entity = _mapper.Map<LocalMessage>(msg);
            entity.ChatId = chatId;
            entity.LocalStatus = 0;
            await context.Messages.AddAsync(entity);
            await context.SaveChangesAsync();
            NotifyNewMessage(entity);
        }
    }

    private async Task UpdateMessageContentAsync(MessageEditResponse msg)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var message = await context.Messages.FindAsync(msg.MessageId);
        if (message != null)
        {
            message.EncryptedContent = msg.NewEncryptedContent;
            message.IsEdited = true;
            message.EditedAt = msg.EditedAt;
            await context.SaveChangesAsync();
            
            NotifyMessageUpdated(message);
        }
    }

    private async Task DeleteMessageAsync(Guid messageId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var message = await context.Messages.FindAsync(messageId);
        if (message != null)
        {
            context.Messages.Remove(message);
            await context.SaveChangesAsync();
            
            OnMessageDeleted?.Invoke(messageId); 
        }
    }
    
    // --- helpers ---
    private void NotifyNewMessage(LocalMessage entity)
    {
        var response = _mapper.Map<MessageResponse>(entity);
        OnNewMessage?.Invoke(response);
    }

    private void NotifyMessageUpdated(LocalMessage entity)
    {
        var response = _mapper.Map<MessageResponse>(entity);
        OnMessageUpdated?.Invoke(response);
    }
    
    private async Task SafeExecute(Func<Task> action)
    {
        try 
        { 
            await action(); 
        }
        catch (Exception ex) 
        { 
            Console.WriteLine($"[Repo Error]: {ex.Message}");
        }
    }

    private string CacheKeyBuild(Guid chatId, bool group = false)
    {
        return $"Messages_{chatId}_{group}";
    }
}
