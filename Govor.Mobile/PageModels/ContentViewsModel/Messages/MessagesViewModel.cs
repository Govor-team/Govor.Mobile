using CommunityToolkit.Mvvm.ComponentModel;

namespace Govor.Mobile.PageModels.ContentViewsModel.Messages;

public partial class MessagesViewModel : ObservableObject
{
    private static readonly LinearGradientBrush _OutgoingBrush =
        new LinearGradientBrush(
            new GradientStopCollection
            {
                new GradientStop(Color.FromArgb("#B35D4A9A"), 0.55f),
                new GradientStop(Color.FromArgb("#A34D5A92"), 0.67f),
                new GradientStop(Color.FromArgb("#913B416F"), 1.0f)
            },
            new Point(0, 0),
            new Point(1, 1));

    private static readonly SolidColorBrush _IncomingBrush =
        new(Color.FromArgb("#A52B3448"));

    private static readonly SolidColorBrush _SelectedOutgoingBrush =
        new(Color.FromArgb("#995F4D87"));

    private static readonly SolidColorBrush _SelectedIncomingBrush =
        new(Color.FromArgb("#9944516D"));

    private static readonly SolidColorBrush _SelectedStroke =
        new(Color.FromArgb("#D9C7FF"));

    private static readonly SolidColorBrush _UnselectedStroke =
        new(Color.FromArgb("#28FFFFFF"));

    [ObservableProperty]
    private Guid id;

    public Guid SenderId { get; init; }
    public DateTime SentAt { get; init; }

    [ObservableProperty]
    private string text = string.Empty;

    [ObservableProperty]
    private string time = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectionStroke))]
    [NotifyPropertyChangedFor(nameof(SelectionStrokeThickness))]
    [NotifyPropertyChangedFor(nameof(BubbleBackground))]
    private bool isSelected;

    [ObservableProperty]
    private bool isSelectionModeVisible;

    [ObservableProperty]
    private AvatarViewModel avatar;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BubbleBackground))]
    [NotifyPropertyChangedFor(nameof(BubbleAlignment))]
    [NotifyPropertyChangedFor(nameof(ShowAvatar))]
    [NotifyPropertyChangedFor(nameof(TextColor))]
    private bool isIncoming;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BubbleMargin))]
    [NotifyPropertyChangedFor(nameof(ShowAvatar))]
    [NotifyPropertyChangedFor(nameof(BubbleCorners))]
    private MessageGroupPosition groupPosition;

    public bool ShowAvatar =>
        IsIncoming &&
        (GroupPosition == MessageGroupPosition.Last ||
         GroupPosition == MessageGroupPosition.Single);

    public Thickness BubbleMargin =>
        GroupPosition == MessageGroupPosition.Middle
            ? new Thickness(0, 2)
            : new Thickness(0, 6);

    public bool HasAttachments => false;

    public Brush BubbleBackground
    {
        get
        {
            if (IsSelected)
            {
                return IsIncoming
                    ? _SelectedIncomingBrush
                    : _SelectedOutgoingBrush;
            }

            return IsIncoming
                ? _IncomingBrush
                : _OutgoingBrush;
        }
    }

    public Brush SelectionStroke =>
        IsSelected
            ? _SelectedStroke
            : _UnselectedStroke;

    public double SelectionStrokeThickness =>
        IsSelected ? 2 : 1;

    public CornerRadius BubbleCorners
    {
        get
        {
            if (IsIncoming)
            {
                return GroupPosition switch
                {
                    MessageGroupPosition.Single =>
                        new CornerRadius(20, 20, 0, 20),

                    MessageGroupPosition.First =>
                        new CornerRadius(20, 20, 5, 20),

                    MessageGroupPosition.Middle =>
                        new CornerRadius(5, 20, 5, 20),

                    MessageGroupPosition.Last =>
                        new CornerRadius(5, 20, 0, 20),

                    _ => new CornerRadius(18)
                };
            }

            return GroupPosition switch
            {
                MessageGroupPosition.Single =>
                    new CornerRadius(20, 20, 20, 0),

                MessageGroupPosition.First =>
                    new CornerRadius(20, 20, 20, 5),

                MessageGroupPosition.Middle =>
                    new CornerRadius(20, 5, 20, 5),

                MessageGroupPosition.Last =>
                    new CornerRadius(20, 5, 20, 0),

                _ => new CornerRadius(18)
            };
        }
    }

    public LayoutOptions BubbleAlignment =>
        IsIncoming
            ? LayoutOptions.Start
            : LayoutOptions.End;

    public Color TextColor => Colors.White;
}
