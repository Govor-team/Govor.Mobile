using Govor.Mobile.Models.Requests;
using Govor.Mobile.Models.Responses;

namespace Govor.Mobile.Services.Interfaces.Repositories;

public interface IMessagesRepository
{
    // --- UI ---
    public event Action<MessageResponse>? OnNewMessage;    
    public event Action<MessageResponse>? OnMessageUpdated; 
    public event Action<Guid>? OnMessageDeleted;        
    
    void Initialize(); 
    Task<List<MessageResponse>> GetMessagesLocalAsync(Guid chatId, int count = 50, bool group = false, Guid startMessage = default);
    Task SyncChatAsync(Guid chatId, bool group = false, int after = 50);
    Task<List<MessageResponse>> LoadHistoryAsync(Guid chatId, Guid? oldestMessageId, int before = 50, bool group = false);
    Task SendMessageAsync(MessageRequest request);
}