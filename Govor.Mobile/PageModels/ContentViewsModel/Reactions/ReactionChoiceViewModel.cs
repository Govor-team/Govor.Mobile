using CommunityToolkit.Mvvm.ComponentModel;
using Govor.Mobile.Models.Reactions;
using Govor.Mobile.Services.Implementations;

namespace Govor.Mobile.PageModels.ContentViewsModel.Reactions;

public partial class ReactionChoiceViewModel(ReactionDefinition definition) : ObservableObject
{
    public ReactionDefinition Definition { get; } = definition;
    public string Text => Definition.DisplayText;
    public string Name => Definition.Name;

    [ObservableProperty]
    private string? mediaPath;

    [ObservableProperty]
    private bool isSelected;

    public bool HasMedia => !string.IsNullOrEmpty(MediaPath);
    public Color Highlight => Color.FromArgb(IsSelected ? "#606E58B0" : "#00FFFFFF");

    partial void OnIsSelectedChanged(bool value) => OnPropertyChanged(nameof(Highlight));
    partial void OnMediaPathChanged(string? value) => OnPropertyChanged(nameof(HasMedia));

    public async Task LoadMediaAsync(ReactionService service)
    {
        if (Definition.Kind == 0)
            return;

        try
        {
            var path = await service.GetMediaPathAsync(Definition);
            await MainThread.InvokeOnMainThreadAsync(() => MediaPath = path);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Reaction preview failed: {ex.Message}");
        }
    }
}
