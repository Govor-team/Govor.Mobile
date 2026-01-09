using CommunityToolkit.Mvvm.ComponentModel;

namespace Govor.Mobile.Models;

public class BackgroundItem : ObservableObject
{
    public string Path { get; init; } = null!;
    public bool IsSystem { get; init; }
    
    public bool CanRemove => !IsSystem;
}