using CommunityToolkit.Mvvm.ComponentModel;

namespace Govor.Mobile.PageModels.ContentViewsModel.Messages;

public partial class MessagesViewModel : ObservableObject
{

    private static readonly LinearGradientBrush _OutgoingBrush = new LinearGradientBrush(
            new GradientStopCollection
            {
                new GradientStop(Color.FromArgb("#7F00FF"), 0.0f),
                new GradientStop(Color.FromArgb("#594aa8"), 0.5f),
                new GradientStop(Color.FromArgb("#313168"), 1.0f)
            },
            new Point(0, 0),
            new Point(1, 1));

    private static readonly SolidColorBrush _IncomingBrush = new SolidColorBrush(Color.FromArgb("#313244"));

    [ObservableProperty]
    private Guid id;
    public Guid SenderId { get; init; }

    [ObservableProperty]
    private string text;

    [ObservableProperty]
    private string time; // Например "14:30"

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

    public bool ShowAvatar => IsIncoming && (GroupPosition == MessageGroupPosition.Last || GroupPosition == MessageGroupPosition.Single);
    public Thickness BubbleMargin =>

    GroupPosition == MessageGroupPosition.Middle
        ? new Thickness(0, 2)
        : new Thickness(0, 6);

    public bool HasAttachments => false;

    // --- UI ЛОГИКА (Computed Properties) ---
    public Brush BubbleBackground => IsIncoming ? _IncomingBrush : _OutgoingBrush;

    public CornerRadius BubbleCorners
    {
        get
        {
            if (IsIncoming)
            {
                // Для входящих сообщений делаем внешний выпуклый уголок со стороны аватарки
                return GroupPosition switch
                {
                    MessageGroupPosition.Single => new CornerRadius(20, 20, 0, 20),
                    MessageGroupPosition.First => new CornerRadius(20, 20, 5, 20),
                    MessageGroupPosition.Middle => new CornerRadius(5, 20, 5, 20),
                    MessageGroupPosition.Last => new CornerRadius(5, 20, 0, 20),
                    _ => new CornerRadius(18)
                };
            }
            else
            {
                return GroupPosition switch
                {
                    MessageGroupPosition.Single => new CornerRadius(20, 20, 20, 0),
                    MessageGroupPosition.First => new CornerRadius(20, 20, 20, 5),
                    MessageGroupPosition.Middle => new CornerRadius(20, 5, 20, 5),
                    MessageGroupPosition.Last => new CornerRadius(20, 5, 20, 0),
                    _ => new CornerRadius(18)
                };
            }
        }
    }

    public LayoutOptions BubbleAlignment => IsIncoming ? LayoutOptions.Start : LayoutOptions.End;
    public Color TextColor => Colors.White;
}

