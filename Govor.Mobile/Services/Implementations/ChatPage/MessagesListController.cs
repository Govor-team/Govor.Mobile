using AutoMapper;
using Govor.Mobile.Models.Requests;
using Govor.Mobile.Models.Responses;
using Govor.Mobile.PageModels.ContentViewsModel;
using Govor.Mobile.PageModels.ContentViewsModel.Messages;
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

    public ObservableRangeCollection<MessagesGroupModel> MessageGroups { get; } = new();
    private HashSet<Guid> _messageIds { get; set; } = new();

    private Guid _currentChatId;
    private Guid _myId;
    private bool _isGroup;
    
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
        _isGroup = isGroup;
        
        // 1. Подгружаем локальные данные
        var localMessages = await _repository.GetMessagesLocalAsync(chatId, group: isGroup);

        MessageGroups.Clear();
        
        var viewModels = localMessages
            .OrderBy(x => x.SentAt)
            .Select(msg => _mapper.Map<MessagesViewModel>(msg, opt => 
            {
                opt.Items["CurrentUserId"] = _myId;
            }))
            .ToList();

        _messageIds = new(viewModels.Select(x => x.Id));

        var groups = CreateGroups(viewModels);

        await MainThread.InvokeOnMainThreadAsync((Action)(() =>
        {
            this.MessageGroups.Clear();
            this.MessageGroups.AddRange(groups);
        }));

        _initialized = true;
        
        // 2. Запускаем фоновую синхронизацию
        _ = _repository.SyncChatAsync(chatId, isGroup);
    }
    
    public async Task<List<MessagesGroupModel>> LoadOlderMessagesAsync(Guid chatId, Guid? oldestMessageId = null)
    {
        // 1. Подгружаем локальные данные
       var localMessages = await _repository.LoadHistoryAsync(chatId, oldestMessageId, group: _isGroup);

       var activeIds = _messageIds;
        
       var viewModels = localMessages
            .OrderBy(x => x.SentAt)
            .Where(x => !activeIds.Contains(x.Id))
            .Select(msg => _mapper.Map<MessagesViewModel>(msg, opt => 
            {
                opt.Items["CurrentUserId"] = _myId;
            }))
            .ToList();

       _messageIds.UnionWith(viewModels.Select(x => x.Id));

       var groups = CreateGroups(viewModels);

       await MainThread.InvokeOnMainThreadAsync((Action)(() =>
       {
           if (groups == null || groups.Count == 0)
               return;

           if (this.MessageGroups.Count > 0)
           {
               var firstExisting = this.MessageGroups.First();
               var lastLoaded = groups.Last();

               // If the last loaded group can join with existing first group, merge them
               if (lastLoaded.IsIncoming == firstExisting.IsIncoming && lastLoaded.SenderId == firstExisting.SenderId)
               {
                   // move existing first group's messages into lastLoaded
                   foreach (var m in firstExisting.Messages)
                       lastLoaded.Messages.Add(m);

                   // recalc positions for merged group
                   var cnt = lastLoaded.Messages.Count;
                   for (int i = 0; i < cnt; i++)
                   {
                       var msg = lastLoaded.Messages[i];
                       if (cnt == 1) msg.GroupPosition = MessageGroupPosition.Single;
                       else if (i == 0) msg.GroupPosition = MessageGroupPosition.First;
                       else if (i == cnt - 1) msg.GroupPosition = MessageGroupPosition.Last;
                       else msg.GroupPosition = MessageGroupPosition.Middle;
                   }

                   // remove the old first group
                   this.MessageGroups.RemoveAt(0);

                   // insert other loaded groups (except lastLoaded) and then lastLoaded
                   var head = groups.Take(groups.Count - 1).ToList();
                   if (head.Count > 0)
                       this.MessageGroups.InsertRange(0, head);

                   this.MessageGroups.Insert(head.Count, lastLoaded);
                   return;
               }
           }

           // default: just insert loaded groups at the beginning
           this.MessageGroups.InsertRange(0, (IEnumerable<MessagesGroupModel>)groups);
       }));
        
       return groups.ToList();
    }

    public async Task<Result<bool>> SendAsync(Guid chatId, string text)
    {
        var request = new MessageRequest 
        { 
            RecipientId = chatId, 
            EncryptedContent = text,
            RecipientType = _isGroup ? RecipientType.Group : RecipientType.User
        };
        
        await _repository.SendMessageAsync(request);
        return Result<bool>.Success(true);
    }
    
    
    private void OnNewMessageReceived(MessageResponse msg)
    {
        if (!IsRelevantChat(msg))
            return;

        MainThread.BeginInvokeOnMainThread((Action)(() =>
        {
            if (_messageIds.Any(x => x == msg.Id))
                return;

            var vm = _mapper.Map<MessagesViewModel>(msg, opt =>
            {
                opt.Items["CurrentUserId"] = _myId;
            });

            // Try append to last group if possible
            var lastGroup = this.MessageGroups.LastOrDefault();
            if (lastGroup != null && lastGroup.IsIncoming == vm.IsIncoming && lastGroup.SenderId == vm.SenderId)
            {
                lastGroup.Messages.Add(vm);

                // update positions for last group
                var count = lastGroup.Messages.Count;
                for (int i = 0; i < count; i++)
                {
                    var m = lastGroup.Messages[i];
                    if (count == 1) m.GroupPosition = MessageGroupPosition.Single;
                    else if (i == 0) m.GroupPosition = MessageGroupPosition.First;
                    else if (i == count - 1) m.GroupPosition = MessageGroupPosition.Last;
                    else m.GroupPosition = MessageGroupPosition.Middle;
                }
            }
            else
            {
                var newGroup = new MessagesGroupModel
                {
                    IsIncoming = vm.IsIncoming,
                    SenderId = vm.SenderId,
                    Avatar = vm.Avatar
                };

                newGroup.Messages.Add(vm);
                vm.GroupPosition = MessageGroupPosition.Single;

                this.MessageGroups.Add(newGroup);
            }

            _messageIds.Add(vm.Id);
        }));
    }
    
    private void OnMessageUpdated(MessageResponse msg)
    {
        if (IsRelevantChat(msg))
        {
            MainThread.BeginInvokeOnMainThread(() => 
            {
                // find existing message in groups
                foreach (var group in this.MessageGroups)
                {
                    var existing = group.Messages.FirstOrDefault(m => m.Id == msg.Id);
                    if (existing != null)
                    {
                        try
                        {
                            _mapper.Map(msg, existing, opt => { opt.Items["CurrentUserId"] = _myId; });
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to map updated message {MessageId}", msg.Id);
                        }

                        // no need to change grouping in common update scenarios
                        return;
                    }
                }

                // if not found - treat as new message arrival
                OnNewMessageReceived(msg);
            });
        }
    }
    
    private void OnMessageDeleted(Guid id)
    {
        MainThread.BeginInvokeOnMainThread(() => 
        {
            for (int gi = 0; gi < this.MessageGroups.Count; gi++)
            {
                var group = this.MessageGroups[gi];
                var msg = group.Messages.FirstOrDefault(m => m.Id == id);
                if (msg != null)
                {
                    group.Messages.Remove(msg);
                    _messageIds.Remove(id);

                    if (group.Messages.Count == 0)
                    {
                        this.MessageGroups.RemoveAt(gi);
                    }
                    else
                    {
                        // recalc positions for this group
                        var cnt = group.Messages.Count;
                        for (int i = 0; i < cnt; i++)
                        {
                            var m2 = group.Messages[i];
                            if (cnt == 1) m2.GroupPosition = MessageGroupPosition.Single;
                            else if (i == 0) m2.GroupPosition = MessageGroupPosition.First;
                            else if (i == cnt - 1) m2.GroupPosition = MessageGroupPosition.Last;
                            else m2.GroupPosition = MessageGroupPosition.Middle;
                        }
                    }

                    _logger.LogInformation("Event: End - message removed: {0}", id);
                    return;
                }
            }
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

    private List<MessagesGroupModel> CreateGroups(List<MessagesViewModel> messages)
    {
        var groups = new List<MessagesGroupModel>();

        foreach (var message in messages)
        {
            var last = groups.LastOrDefault();
            var resultGroup = AddMessageToGroups(message, last);

            if (ReferenceEquals(resultGroup, last))
            {
                // already added to last
            }
            else
            {
                // new group created -> append
                groups.Add(resultGroup);
            }
        }

        // compute positions for messages in each group
        foreach (var g in groups)
        {
            var count = g.Messages.Count;
            for (int i = 0; i < count; i++)
            {
                var msg = g.Messages[i];
                if (count == 1)
                    msg.GroupPosition = MessageGroupPosition.Single;
                else if (i == 0)
                    msg.GroupPosition = MessageGroupPosition.First;
                else if (i == count - 1)
                    msg.GroupPosition = MessageGroupPosition.Last;
                else
                    msg.GroupPosition = MessageGroupPosition.Middle;
            }
        }

        return groups;
    }

    private MessagesGroupModel AddMessageToGroups(MessagesViewModel message, MessagesGroupModel lastGroup)
    {
        bool canJoin =
            lastGroup != null &&
            lastGroup.IsIncoming == message.IsIncoming &&
            lastGroup.SenderId == message.SenderId;
            

        if (canJoin)
        {
            lastGroup.Messages.Add(message);
            return lastGroup;
        }

        var group = new MessagesGroupModel
        {
            IsIncoming = message.IsIncoming,
            Avatar = message.Avatar,
            SenderId = message.SenderId,
        };

        group.Messages.Add(message);

        return group;
    }

    public void Dispose()
    {
        _repository.OnNewMessage -= OnNewMessageReceived; 
        _repository.OnMessageUpdated -= OnMessageUpdated;
        _repository.OnMessageDeleted -= OnMessageDeleted;
    }
}