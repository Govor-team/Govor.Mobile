using Govor.Mobile.PageModels.MainFlow.Groups;
using Govor.Mobile.Pages.ContentViews;

namespace Govor.Mobile.Pages.MainFlow;

public partial class GroupProfilePage : SmoothBackPage, IQueryAttributable
{
    private GlassActionSheet? _memberSheet;
    private bool _visible;
    public GroupProfileModel ViewModel { get; }

    public GroupProfilePage(GroupProfileModel model)
    {
        ViewModel = model;
        InitializeComponent(); BindingContext = model;
        model.SelectMemberAction = SelectMemberActionAsync;
        model.ConfirmMemberAction = ConfirmMemberActionAsync;
        model.InvalidateMemberMenu += CloseMemberSheet;
        model.AccessLost += CloseCommunity;
    }
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("groupId", out var value) && Guid.TryParse(value.ToString(), out var id)) ViewModel.GroupId = id;
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing(); _visible = true;
        ViewModel.Activate();
        await ViewModel.LoadAsync();
    }
    protected override void OnDisappearing()
    {
        _visible = false; ViewModel.Deactivate(); CloseMemberSheet();
        base.OnDisappearing();
    }
    private Task<string?> SelectMemberActionAsync(GroupMemberItem item, IReadOnlyList<string> options) =>
        ShowSheetAsync(new GlassActionSheet(item.Row, "", "Действия участника",
            options.Select(title => new GlassSheetAction(title, title, title == "Заблокировать", title != "Заявка отправлена")).ToArray()));
    private async Task<bool> ConfirmMemberActionAsync(string title, string detail) =>
        await ShowSheetAsync(new GlassActionSheet(null, title, detail,
            new[] { new GlassSheetAction("confirm", "Подтвердить", title == "Заблокировать" || title == "Передать владение") })) == "confirm";
    private async Task<string?> ShowSheetAsync(GlassActionSheet sheet)
    {
        CloseMemberSheet(); _memberSheet = sheet;
        var selected = await sheet.ShowAsync(PageLayout);
        if (_memberSheet == sheet) _memberSheet = null;
        return selected;
    }
    private void CloseMemberSheet() { _memberSheet?.Close(); _memberSheet = null; }
    protected override bool OnBackButtonPressed()
    {
        if (_memberSheet != null) { CloseMemberSheet(); return true; }
        return base.OnBackButtonPressed();
    }
    private async void CloseCommunity()
    {
        if (!_visible) return;
        CloseMemberSheet();
        // Remove a potentially underlying chat as well as the profile after membership loss.
        await Shell.Current.GoToAsync("//root", false);
    }
    private async void BackClicked(object? sender, EventArgs args) => await Shell.Current.GoToAsync("..", false);
}
