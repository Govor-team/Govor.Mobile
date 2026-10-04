using Govor.Mobile.Models.Groups;
using Govor.Mobile.PageModels.ContentViewsModel;
using Govor.Mobile.PageModels.MainFlow.Groups;
using Govor.Mobile.Services.Interfaces.Profiles;

namespace Govor.Mobile.Services.Implementations;

public sealed class GroupRowFactory(IServiceProvider services, GroupMediaService media, IUserProfileService profiles)
{
    public UserListItemViewModel Create(string name)
    {
        var avatar = services.GetRequiredService<AvatarViewModel>();
        // With no image this completes synchronously and sets the generated initials/color.
        _ = avatar.InitializeAsync(name, null);
        return new UserListItemViewModel(avatar, null!) { Title = name, Subtitle = "", DateTime = "" };
    }

    public async Task UpdateGroupAsync(GroupProfile group, UserListItemViewModel row)
    {
        row.Title = group.Name;
        await row.Avatar.InitializeAsync(group.Name, null);
        row.Avatar.AvatarImage = await media.LoadAsync(group);
    }

    public async Task<GroupMemberItem> CreateMemberAsync(GroupMember member)
    {
        var row = Create(member.Username);
        row.Subtitle = member.RoleLabel;
        try
        {
            // The documented member DTO contains no avatar; use the existing user profile API.
            var profile = await profiles.GetProfileAsync(member.UserId);
            await row.Avatar.InitializeAsync(member.Username, profile.IconId);
            row.IsOnline = !member.IsBanned && profile.IsOnline;
        }
        catch { /* A unavailable user profile must not hide the member or their current role. */ }
        return new GroupMemberItem(member, row);
    }
}
