using CommunityToolkit.Mvvm.ComponentModel;
namespace Govor.Mobile.PageModels.ContentViewsModel.Messages;
public partial class ReactionChip : ObservableObject
{
    public Guid MessageId { get; init; }
    public Guid? ReactionId { get; init; }
    [ObservableProperty] private string text = "";
    public string CountText { get; init; } = "";
    public bool IsMine { get; init; }
    public Color Background => Color.FromArgb(IsMine ? "#805F4BB7" : "#602B3448");
    [ObservableProperty] private ImageSource? image;
    public bool HasImage => Image != null;
    partial void OnImageChanged(ImageSource? value) => OnPropertyChanged(nameof(HasImage));
}
