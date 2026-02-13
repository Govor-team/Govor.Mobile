using System.Collections.ObjectModel;
using AutoMapper;
using Govor.Mobile.Models.Requests;
using Govor.Mobile.Models.Responses;
using Govor.Mobile.PageModels.ContentViewsModel;
using Govor.Mobile.Services.Interfaces;
using Govor.Mobile.Services.Interfaces.ChatPage;
using Govor.Mobile.Services.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Govor.Mobile.Services.Implementations.ChatPage;

public class MessagesListController : IMessagesListController, IDisposable
{
    private readonly IMessagesRepository _repository;
    private readonly IMessageStore _store;
    private readonly ILogger<MessagesListController> _logger;
    private readonly IMapper _mapper;
    private bool _initialized = false;
    public ObservableCollection<MessagesViewModel> Messages { get; private set; } = new();

    private Guid _currentChatId;
    private Guid _myId;
    
    public MessagesListController(
        IMessagesRepository repository,
        IMessageStore store,
        ILogger<MessagesListController> logger,
        IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
        _store = store;
        _logger = logger;
    }
    
    public async Task InitializeAsync(Guid chatId, Guid currentUserId, bool isGroup)
    {
        _currentChatId = chatId;
        _myId =  currentUserId;
        
        if (_initialized)
            return;
        
        _repository.OnNewMessage += OnNewMessageReceived;
        _repository.OnMessageUpdated += OnMessageUpdated;
        _repository.OnMessageDeleted += OnMessageDeleted;
        
        // 1. Подгружаем локальные данные
        var localMessages = await _repository.GetMessagesLocalAsync(chatId, group: isGroup);
        
        Messages.Clear();
        
        foreach (var msg in localMessages.OrderBy(x => x.SentAt))
        {
            // Передаем myId в контекст каждого маппинга
            var vm = _mapper.Map<MessagesViewModel>(msg, opt => 
            {
                opt.Items["CurrentUserId"] = _myId;
            });

            MainThread.BeginInvokeOnMainThread(() => 
            {
                if(!Messages.Contains(vm))
                    Messages.Add(vm);
            });
        }

        _initialized = true;
        
        // 2. Запускаем фоновую синхронизацию
        _ = _repository.SyncChatAsync(chatId, isGroup);
    }

    public async Task<Result<bool>> SendAsync(Guid chatId, string text, bool isGroup)
    {
        var request = new MessageRequest 
        { 
            RecipientId = chatId, 
            EncryptedContent = text,
            RecipientType = isGroup ? RecipientType.Group : RecipientType.User
        };
        
        await _repository.SendMessageAsync(request);
        return Result<bool>.Success(true);
    }
    
    
    private void OnNewMessageReceived(MessageResponse msg)
    {
        if (!IsRelevantChat(msg))
            return;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (Messages.Any(x => x.Id == msg.Id))
                return;

            var vm = _mapper.Map<MessagesViewModel>(msg, opt =>
            {
                opt.Items["CurrentUserId"] = _myId;
            });

            Messages.Add(vm);
        });
    }

    
    private void OnMessageUpdated(MessageResponse msg)
    {
        if (IsRelevantChat(msg))
        {
            MainThread.BeginInvokeOnMainThread(() => 
            {
                var item = Messages.FirstOrDefault(x => x.Id == msg.Id);
                
                if (item != null)
                {
                    
                }
            });
        }
    }
    
    private void OnMessageDeleted(Guid id)
    {
        MainThread.BeginInvokeOnMainThread(() => 
        {
            var item = Messages.FirstOrDefault(x => x.Id == id);
            if (item != null)
                Messages.Remove(item);
            _logger.LogInformation("Event: End - message remove: {0}", id);
        });
    }

    private bool IsRelevantChat(MessageResponse msg)
    {
        bool isRelevant = _currentChatId == msg.RecipientId;
        
        if (!isRelevant)
        {
            // Этот лог покажет вам, ПОЧЕМУ сообщение отвергнуто
            _logger.LogWarning($"Msg rejected. ChatId: {_currentChatId}, MyId: {_myId}. " +
                               $"Msg Sender: {msg.SenderId}, Msg Recipient: {msg.RecipientId}");
        }
        return isRelevant;
    }

    public void Dispose()
    {
        _repository.OnNewMessage -= OnNewMessageReceived; 
        _repository.OnMessageUpdated -= OnMessageUpdated;
        _repository.OnMessageDeleted -= OnMessageDeleted;
    }
}