using CommunityToolkit.Mvvm.Input;
using Govor.Mobile.PageModels.ContentViewsModel;

namespace Govor.Mobile.Services.Interfaces.ChatPage;

public interface IChatHeaderService
{
    Task<ChatHeaderViewModel> BuildAsync(Guid id, bool isGroup, IAsyncRelayCommand backCommand);
}