using Govor.Mobile.Models.Groups;
using Govor.Mobile.PageModels.ContentViewsModel;

namespace Govor.Mobile.PageModels.MainFlow.Groups;

public sealed record GroupMemberItem(GroupMember Member, UserListItemViewModel Row)
{
    public Guid UserId => Member.UserId;
}
