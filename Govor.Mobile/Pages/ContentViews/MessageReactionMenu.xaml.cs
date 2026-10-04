using Govor.Mobile.PageModels.ContentViewsModel.Reactions;

namespace Govor.Mobile.Pages.ContentViews;

public partial class MessageReactionMenu : ContentView
{
    public MessageReactionMenuModel ViewModel { get; }

    public MessageReactionMenu(MessageReactionMenuModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        BindingContext = ViewModel;
        ViewModel.PropertyChanged += (_, _) => Dispatcher.Dispatch(UpdateCardSize);
    }
    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        if (width <= 0 || height <= 0)
            return;

        UpdateCardSize();
    }

    private void UpdateCardSize()
    {
        if (Width <= 0 || Height <= 0)
            return;

        var cardWidth = Math.Min(320, Math.Max(0, Width - 32));
        var contentSize = ((IView)MenuContent).Measure(Math.Max(0, cardWidth - 24), double.PositiveInfinity);
        MenuCard.WidthRequest = cardWidth;
        MenuCard.HeightRequest = Math.Min(Math.Ceiling(contentSize.Height) + 24, Math.Min(440, Math.Max(0, Height - 32)));
    }
}
