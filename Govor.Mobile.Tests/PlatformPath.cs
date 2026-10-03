global using Govor.Mobile.Models;
global using Govor.Mobile.Models.Requests;

// Stand-in for the platform path only; production database/cache code is linked unchanged.
public static class FileSystem
{
    public static string AppDataDirectory { get; } = Path.Combine(Path.GetTempPath(), "Govor.Mobile.Tests", Guid.NewGuid().ToString("N"));
    static FileSystem() => Directory.CreateDirectory(AppDataDirectory);
}
