using Govor.Mobile.Models.Groups;
using Govor.Mobile.PageModels.ContentViewsModel.Messages;
using Govor.Mobile.Pages.ContentViews;
using Govor.Mobile.Services.Implementations;
using Govor.Mobile.Services.Interfaces;

namespace Govor.Mobile.Pages.MainFlow;

public partial class ChatPage
{
    private readonly GroupMemberContactService _contacts;
    private readonly IUserListItemViewModelFactory _users;
    private GlassActionSheet? _userProfileSheet;
    private bool _loadingProfile;

    private async void SenderAvatarTapped(object? sender, TappedEventArgs args)
    {
        if (_loadingProfile || sender is not BindableObject { BindingContext: MessagesViewModel message }
            || message.IsChannel || !_pageVisible) return;
        _loadingProfile = true;
        CloseMessageMenu(); CloseUserProfile();
        MessageEditor.Unfocus();
        try
        {
            var profileTask = _users.CreateAsync(message.SenderId);
            var contactTask = _contacts.GetStateAsync(message.SenderId);
            await Task.WhenAll(profileTask, contactTask);
            if (!_pageVisible) return;
            var profile = await profileTask;
            var contact = await contactTask;
            var description = profile.Subtitle;
            profile.Subtitle = profile.IsOnline ? "В сети" : "Не в сети";
            var actions = contact.Kind == MemberContactKind.Self ? Array.Empty<GlassSheetAction>()
                : new[] { new GlassSheetAction("contact", contact.ActionTitle, Enabled: contact.CanAct) };
            var result = await ShowUserProfileAsync(new GlassActionSheet(profile, "", description, actions));
            if (result != "contact" || !contact.CanAct || !_pageVisible) return;
            var status = await _contacts.ActAsync(message.SenderId, contact.Kind);
            if (_pageVisible && !string.IsNullOrEmpty(status))
                await ShowUserProfileAsync(new GlassActionSheet(profile, "", status, Array.Empty<GlassSheetAction>()));
        }
        catch (Exception ex)
        {
            if (_pageVisible)
                await ShowUserProfileAsync(new GlassActionSheet(null, "Профиль пользователя", ex.Message, Array.Empty<GlassSheetAction>()));
        }
        finally { _loadingProfile = false; }
    }

    private async Task<string?> ShowUserProfileAsync(GlassActionSheet sheet)
    {
        if (Content is not Grid host) return null;
        CloseUserProfile(); _userProfileSheet = sheet;
        var action = await sheet.ShowAsync(host);
        if (_userProfileSheet == sheet) _userProfileSheet = null;
        return action;
    }
    private void CloseUserProfile()
    { _userProfileSheet?.Close(); _userProfileSheet = null; }
}
