using Govor.Mobile.PageModels.MainFlow.Groups;
namespace Govor.Mobile.Pages.MainFlow.Layouts;
public partial class GroupsHomeView : ContentView
{
    public GroupsHomeModel? ViewModel => BindingContext as GroupsHomeModel;
    public GroupsHomeView() => InitializeComponent();
    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();
        OnPropertyChanged(nameof(ViewModel));
    }
}
