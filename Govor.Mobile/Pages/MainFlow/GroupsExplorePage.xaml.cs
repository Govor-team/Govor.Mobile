using Govor.Mobile.PageModels.MainFlow.Groups;
namespace Govor.Mobile.Pages.MainFlow;
public partial class GroupsExplorePage : SmoothBackPage, IQueryAttributable
{
    public GroupsExploreModel ViewModel { get; }
    public GroupsExplorePage(GroupsExploreModel model) { ViewModel = model; InitializeComponent(); BindingContext = model; }
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (!query.TryGetValue("invite", out var value)) return;
        ViewModel.Section = 1;
        ViewModel.InvitationInput = value.ToString() ?? "";
        ViewModel.PreviewInviteCommand.Execute(null);
    }
    private async void BackClicked(object? sender, EventArgs args) => await Shell.Current.GoToAsync("..", false);
}
