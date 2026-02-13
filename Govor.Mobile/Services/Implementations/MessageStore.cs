using System.Collections.ObjectModel;
using AutoMapper;
using Govor.Mobile.Models.Responses;
using Govor.Mobile.PageModels.ContentViewsModel;
using Govor.Mobile.Services.Interfaces;
using Govor.Mobile.Services.Interfaces.Repositories;
using Microsoft.Extensions.Caching.Memory;

namespace Govor.Mobile.Services.Implementations;

public class MessageStore : IMessageStore
{
    private readonly IMemoryCache _cache;
    private readonly IMapper _mapper;
    private readonly IMessagesRepository _repository;

    private readonly MemoryCacheEntryOptions _options = new()
    {
        SlidingExpiration = TimeSpan.FromMinutes(30),
        Priority = CacheItemPriority.High
    };

    public MessageStore(
        IMemoryCache cache,
        IMessagesRepository repository,
        IMapper mapper)
    {
        _cache = cache;
        _repository = repository;
        _mapper = mapper;
        
        _repository.Initialize();
    }

    private string GetKey(Guid chatId, bool isGroup) => $"chat_{chatId}_{isGroup}";

    public async Task<ObservableCollection<MessagesViewModel>> 
        GetOrLoadChatAsync(Guid chatId, Guid currentUserId, bool isGroup)
    {
        if (_cache.TryGetValue(GetKey(chatId, isGroup), out ObservableCollection<MessagesViewModel>? existing))
            return existing;

        // Грузим из SQLite
        var localMessages = await _repository
            .GetMessagesLocalAsync(chatId, group: isGroup);

        var collection = new ObservableCollection<MessagesViewModel>();

        foreach (var msg in localMessages.OrderBy(x => x.SentAt))
        {
            var vm = _mapper.Map<MessagesViewModel>(msg, opt =>
            {
                opt.Items["CurrentUserId"] = currentUserId;
            });
            
            MainThread.BeginInvokeOnMainThread(() => 
            {
                if(!collection.Contains(vm))
                    collection.Add(vm);
            });
        }

        // Кладем в кэш
        _cache.Set(GetKey(chatId, isGroup), collection, _options);
        
        _ = _repository.SyncChatAsync(chatId, isGroup);
        
        return collection;
    }

    public void AddOrUpdate(MessageResponse msg, Guid currentUserId)
    {
        if (!_cache.TryGetValue(GetKey(msg.RecipientId, msg.RecipientType == RecipientType.Group),
                out ObservableCollection<MessagesViewModel>? collection))
            return;

        var existing = collection?.FirstOrDefault(x => x.Id == msg.Id);

        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (existing != null)
            {
                //TODO: make existing.UpdateFrom(msg);
            }
            else
            {
                var vm = _mapper.Map<MessagesViewModel>(msg, opt =>
                {
                    opt.Items["CurrentUserId"] = currentUserId;
                });

                collection?.Add(vm);
            }
        });
    }


    public void Remove(Guid messageId, Guid chatId, bool isGroup)
    {
        var key = GetKey(chatId, isGroup);

        if (!_cache.TryGetValue(key, out ObservableCollection<MessagesViewModel>? collection))
            return;

        var item = collection?.FirstOrDefault(x => x.Id == messageId);

        if (item != null)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                collection?.Remove(item);
            });
        }
    }

    public void ClearChat(Guid chatId, bool isGroup)
    {
        _cache.Remove(GetKey(chatId, isGroup));
    }
}