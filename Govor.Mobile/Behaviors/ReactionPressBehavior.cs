using System.Windows.Input;
using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.Core;
using Govor.Mobile.PageModels.ContentViewsModel.Messages;
using Govor.Mobile.PageModels.ContentViewsModel.Reactions;
using Govor.Mobile.Pages.ContentViews;

namespace Govor.Mobile.Behaviors;

/// <summary>Short presses perform the action; holding shows a preview until release.</summary>
public sealed class ReactionPressBehavior : Behavior<VisualElement>
{
    public static readonly BindableProperty TapCommandProperty = BindableProperty.Create(
        nameof(TapCommand), typeof(ICommand), typeof(ReactionPressBehavior));

    public ICommand? TapCommand
    {
        get => (ICommand?)GetValue(TapCommandProperty);
        set => SetValue(TapCommandProperty, value);
    }

    private VisualElement? _target;
    private TouchBehavior? _touch;
    private Grid? _host;
    private ContentPage? _page;
    private ReactionPreviewView? _preview;
    private bool _held;

    protected override void OnAttachedTo(VisualElement target)
    {
        base.OnAttachedTo(target);
        _target = target;
        _touch = new TouchBehavior
        {
            LongPressDuration = 450,
            ShouldMakeChildrenInputTransparent = true,
            Command = new Command(OnTap),
            LongPressCommand = new Command(ShowPreview)
        };
        _touch.CurrentTouchStatusChanged += TouchStatusChanged;
        target.Behaviors.Add(_touch);
    }

    protected override void OnDetachingFrom(VisualElement target)
    {
        ClosePreview();
        if (_touch != null)
        {
            _touch.CurrentTouchStatusChanged -= TouchStatusChanged;
            target.Behaviors.Remove(_touch);
        }
        _touch = null;
        _target = null;
        base.OnDetachingFrom(target);
    }

    private void OnTap()
    {
        var parameter = _target?.BindingContext;
        if (!_held && TapCommand?.CanExecute(parameter) == true)
            TapCommand.Execute(parameter);
    }

    private void TouchStatusChanged(object? sender, TouchStatusChangedEventArgs args)
    {
        if (args.Status == TouchStatus.Started)
            _held = false;
        else
            ClosePreview();
    }

    private void ShowPreview()
    {
        _held = true;
        if (_target == null || _touch?.CurrentTouchStatus != TouchStatus.Started)
            return;

        var model = _target.BindingContext switch
        {
            ReactionChoiceViewModel choice => new ReactionPreviewModel(
                choice.Name, choice.Text,
                choice.HasMedia ? ImageSource.FromFile(choice.MediaPath!) : null),
            ReactionChip chip => new ReactionPreviewModel("Реакция", chip.Text, chip.Image),
            _ => null
        };
        if (model == null)
            return;

        Element? ancestor = _target;
        while (ancestor != null && ancestor is not ContentPage)
            ancestor = ancestor.Parent;
        if (ancestor is not ContentPage page || page.Content is not Grid host)
            return;

        ClosePreview();
        _page = page;
        _host = host;
        _preview = new ReactionPreviewView(model);
        Grid.SetRowSpan(_preview, Math.Max(1, host.RowDefinitions.Count));
        Grid.SetColumnSpan(_preview, Math.Max(1, host.ColumnDefinitions.Count));
        host.Children.Add(_preview);
        page.Disappearing += PageDisappearing;
    }

    private void PageDisappearing(object? sender, EventArgs args) => ClosePreview();

    private void ClosePreview()
    {
        if (_page != null)
            _page.Disappearing -= PageDisappearing;
        if (_preview != null)
            _host?.Children.Remove(_preview);
        _preview = null;
        _host = null;
        _page = null;
    }
}
