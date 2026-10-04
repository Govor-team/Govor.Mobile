using System.Windows.Input;

namespace Govor.Mobile.Pages.ContentViews;

public partial class ReactionChoiceView : ContentView
{
    public static readonly BindableProperty TapCommandProperty = BindableProperty.Create(
        nameof(TapCommand), typeof(ICommand), typeof(ReactionChoiceView));

    public ICommand? TapCommand
    {
        get => (ICommand?)GetValue(TapCommandProperty);
        set => SetValue(TapCommandProperty, value);
    }

    public ReactionChoiceView() => InitializeComponent();
}
