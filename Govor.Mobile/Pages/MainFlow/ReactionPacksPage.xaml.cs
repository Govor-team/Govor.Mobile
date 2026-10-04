using Govor.Mobile.PageModels.MainFlow;
using Govor.Mobile.Services.Implementations;

namespace Govor.Mobile.Pages.MainFlow;

public partial class ReactionPacksPage : SmoothBackPage
{
    public ReactionPacksPageModel ViewModel { get; }

    public ReactionPacksPage(ReactionService service)
    {
        ViewModel = new ReactionPacksPageModel(service);
        InitializeComponent();
        BindingContext = ViewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await ViewModel.LoadAsync();
    }

    private async void CloseClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync("..", false);
}
