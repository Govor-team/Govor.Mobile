using System.ComponentModel;
using Govor.Mobile.PageModels.ContentViewsModel;
using Govor.Mobile.PageModels.MainFlow;

namespace Govor.Mobile.Pages.ContentViews;

public partial class ChatHeaderView : ContentView
{
    private ChatPageModel? _selectionModel;
    public ChatPageModel? SelectionModel
    {
        get => _selectionModel;
        set
        {
            if (_selectionModel is not null) _selectionModel.PropertyChanged -= OnSelectionChanged;
            _selectionModel = value;
            SelectionActions.BindingContext = value;
            SelectionCount.BindingContext = value;
            if (value is not null) value.PropertyChanged += OnSelectionChanged;
            UpdateSelection();
        }
    }

    public ChatHeaderView() => InitializeComponent();

    private void OnSelectionChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(ChatPageModel.IsSelectionMode))
            MainThread.BeginInvokeOnMainThread(UpdateSelection);
    }

    private void UpdateSelection()
    {
        var selecting = _selectionModel?.IsSelectionMode == true;
        ProfileActions.IsVisible = !selecting;
        SelectionActions.IsVisible = selecting;
        ProfileSubtitle.IsVisible = !selecting;
        SelectionCount.IsVisible = selecting;
        if (selecting)
        {
            BackButton.RemoveBinding(ImageButton.CommandProperty);
            BackButton.Command = _selectionModel!.ExitMessageSelectionCommand;
        }
        else BackButton.SetBinding(ImageButton.CommandProperty, nameof(ChatHeaderViewModel.GoBackCommand));
    }
}
