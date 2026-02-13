using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Govor.Mobile.PageModels.ContentViewsModel;

public partial class MessagesViewModel : ObservableObject
{
    [ObservableProperty]
    private Guid id;

    [ObservableProperty]
    private string text;

    [ObservableProperty]
    private string time; // Например "14:30"

    [ObservableProperty]
    private AvatarViewModel avatar;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BubbleColor))]
    [NotifyPropertyChangedFor(nameof(BubbleAlignment))]
    [NotifyPropertyChangedFor(nameof(TextColor))]
    private bool isIncoming;

    public bool HasAttachments => false;

    // --- UI ЛОГИКА (Computed Properties) ---
    public Color BubbleColor => IsIncoming ? Color.FromArgb("#3E4152") : Color.FromArgb("#4A85CC");
    public LayoutOptions BubbleAlignment => IsIncoming ? LayoutOptions.Start : LayoutOptions.End;
    public Color TextColor => Colors.White; 
}