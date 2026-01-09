using System.Text.Json;
using Govor.Mobile.Models;
using Govor.Mobile.Services.Interfaces;
using FileResult = Microsoft.Maui.Storage.FileResult;

namespace Govor.Mobile.Services.Implementations;

public class BackgroundService : IBackgroundImageService
{
    private const string CurrentBackgroundKey = "UserBackgroundImage";
    private const string CustomListKey = "CustomBackgroundsList";

    private readonly List<string> _presets = new(){
            "background_flag.png", 
            "background_girls.png", 
            "vedu_theme.png", 
            "rod_theme.png",
            "aria_theme.png"
        };

    private readonly List<BackgroundItem> _defaultBackgrounds = new();

    public BackgroundService()
    {
        foreach (var path in _presets)
        {
            _defaultBackgrounds.Add(new BackgroundItem() { Path = path, IsSystem = true });
        }
    }

    
    public List<BackgroundItem> GetAvailableBackgrounds()
    {
        var allBackgrounds = new List<string>();
        
        var result = new List<BackgroundItem>(_defaultBackgrounds);
        
        var json = Preferences.Default.Get(CustomListKey, string.Empty);
        if (!string.IsNullOrEmpty(json))
        {
            var savedPaths = JsonSerializer.Deserialize<List<string>>(json);
            if (savedPaths != null) allBackgrounds.AddRange(savedPaths);
        }

        foreach (var allBackground in allBackgrounds)
        {
            result.Add(new BackgroundItem(){IsSystem = false, Path = allBackground});
        }
        
        return result;
    }

    public async Task<BackgroundItem> AddBackgroundFromGallery(FileResult file)
    {
        var localPath = Path.Combine(FileSystem.AppDataDirectory, file.FileName);

        using (var stream = await file.OpenReadAsync())
        using (var newStream = File.OpenWrite(localPath))
        {
            await stream.CopyToAsync(newStream);
        }
        
        var json = Preferences.Default.Get(CustomListKey, "[]");
        var savedPaths = JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();

        if (!savedPaths.Contains(localPath))
        {
            savedPaths.Add(localPath);
            Preferences.Default.Set(CustomListKey, JsonSerializer.Serialize(savedPaths));
        }

        return new BackgroundItem(){IsSystem = false, Path = localPath};
    }

    public async Task<bool> RemoveBackgroundAsync(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
                
                var json = Preferences.Default.Get(CustomListKey, "[]");
                var savedPaths = JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();

                if (savedPaths.Contains(path))
                {
                    savedPaths.Remove(path);
                    Preferences.Default.Set(CustomListKey, JsonSerializer.Serialize(savedPaths));
                }

                return true;
            }
            return false;
        }
        catch (IOException ex)
        {
            return false;
        }
        catch (Exception ex)
        {
            return false;
        }
    }

    public void ApplyBackground(string path)
    {
        Preferences.Default.Set(CurrentBackgroundKey, path);
        Application.Current.Resources["CurrentBackground"] = ImageSource.FromFile(path);
    }

    public BackgroundItem LoadCurrent()
    {
        var path = Preferences.Default.Get(CurrentBackgroundKey, string.Empty);
        
        bool isSystem = _presets.Contains(path) || string.IsNullOrEmpty(path);

        if (string.IsNullOrEmpty(path))
        {
            var theme = Application.Current?.RequestedTheme ?? AppTheme.Light;
            path = theme == AppTheme.Dark ? "background_girls.png" : "background_flag.png";
            isSystem = true; 
        }

        Application.Current.Resources["CurrentBackground"] = ImageSource.FromFile(path);
        
        return new BackgroundItem(){ Path = path, IsSystem = isSystem }; 
    }
}