using FileResult = Microsoft.Maui.Storage.FileResult;

namespace Govor.Mobile.Services;

/// <summary>Preserves a provider stream instead of treating a content URI as a disk path.</summary>
public sealed record SelectedAttachment(string FileName, Func<Task<Stream>> OpenReadAsync)
{
    public static SelectedAttachment FromFile(FileResult file) => new(file.FileName, file.OpenReadAsync);
}
