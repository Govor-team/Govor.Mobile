using System.Windows.Input;
using Govor.Mobile.PageModels.ContentViewsModel;

namespace Govor.Mobile.Pages.ContentViews;

public sealed record GlassSheetAction(string Id, string Title, bool Destructive = false, bool Enabled = true)
{
    public Color TextColor => Destructive ? Color.FromArgb("#FFB2BE") : Colors.White;
}

public partial class GlassActionSheet : ContentView
{
    private readonly TaskCompletionSource<string?> _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public UserListItemViewModel? MemberRow { get; }
    public string Heading { get; }
    public string Detail { get; }
    public bool HasMember => MemberRow != null;
    public bool HasHeading => Heading.Length > 0;
    public bool HasDetail => Detail.Length > 0;
    public IReadOnlyList<GlassSheetAction> Actions { get; }
    public ICommand ChooseCommand { get; }

    public GlassActionSheet(UserListItemViewModel? member, string heading, string detail, IReadOnlyList<GlassSheetAction> actions)
    {
        MemberRow = member; Heading = heading; Detail = detail; Actions = actions;
        ChooseCommand = new Command<string>(id => Close(id));
        InitializeComponent();
        BindingContext = this;
        SizeChanged += (_, _) =>
        {
            Card.WidthRequest = Math.Max(0, Math.Min(340, Width - 32));
            var contentHeight = (HasMember ? 84 : 0) + (HasHeading ? 34 : 0) + (HasDetail ? 42 : 0)
                + Actions.Count * 52 + 68;
            Card.HeightRequest = Math.Max(0, Math.Min(contentHeight, Height - 48));
        };
    }

    public Task<string?> ShowAsync(Grid host)
    {
        Grid.SetRowSpan(this, Math.Max(1, host.RowDefinitions.Count));
        host.Children.Add(this);
        return _completion.Task;
    }
    public void Close(string? action = null)
    {
        if (Parent is Grid host) host.Children.Remove(this);
        _completion.TrySetResult(action);
    }
    private void CancelClicked(object? sender, EventArgs args) => Close();
    private void DismissTapped(object? sender, TappedEventArgs args) => Close();
}
