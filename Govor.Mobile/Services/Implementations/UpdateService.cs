using System.ComponentModel;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using Govor.Mobile.Services.Interfaces;

namespace Govor.Mobile.Services.Implementations;

public partial class UpdateService : IUpdateService, IConnectivityChanged
{ 
    private const string GitHubRepo = "Govor-team/Govor.Mobile";
    private const string AssetNameWindows = "Govor.msix"; // Asset name for Windows
    private const string AssetNameAndroid = "Govor.apk"; // Asset name for Android
    
    public event Action<bool>? UpdateAvailabilityChanged;
    public bool HasNewUpdate { private set; get; }
    private GitHubRelease? _latestRelease;
    
    public async Task<bool> CheckForUpdatesAsync()
    {
        try
        {
            var currentVersion = AppInfo.VersionString; // Or use Assembly.GetExecutingAssembly().GetName().Version.ToString()

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("YourApp", currentVersion));

            var response = await httpClient.GetAsync($"https://api.github.com/repos/{GitHubRepo}/releases/latest");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var release = JsonSerializer.Deserialize<GitHubRelease>(json);

            if (release == null || string.IsNullOrEmpty(release.TagName)) return false;
            
            _latestRelease = release;
            
            var latestVersion = release.TagName.TrimStart('v'); // Assuming tags like 'v1.0.1'

            if (Version.Parse(latestVersion) > Version.Parse(currentVersion))
            {
                return true;
                
                var update = await Application.Current.MainPage.DisplayAlert("Update Available",
                    $"A new version {latestVersion} is available. Do you want to update?", "Yes", "No");

                if (update)
                {
                    string assetName = GetPlatformAssetName();
                    var asset = release.Assets?.FirstOrDefault(a => a.Name == assetName);

                    if (asset != null)
                        await DownloadAndInstallAsync(asset.BrowserDownloadUrl, assetName);
                    else
                        await Application.Current.MainPage.DisplayAlert("Error",
                            "No suitable asset found for this platform.", "OK");
                }
            }
            
            return false;
        }
        catch (Exception ex)
        {
            // Handle exception, e.g., log or show message
            Console.WriteLine($"Update check failed: {ex.Message}");
            return false;
        }
    }
    
    
    private string GetPlatformAssetName()
    {
#if WINDOWS
            return AssetNameWindows;
#elif ANDROID
            return AssetNameAndroid;
// Add other platforms
#else
        throw new NotSupportedException("Platform not supported for updates.");
#endif
    }
    
    private async Task DownloadAndInstallAsync(string downloadUrl, string fileName)
    {
        try
        {
            using var httpClient = new HttpClient();
            var response = await httpClient.GetAsync(downloadUrl);
            response.EnsureSuccessStatusCode();

            // Get a temporary path
            var downloadPath = Path.Combine(FileSystem.CacheDirectory, fileName);

            await using var fileStream = File.OpenWrite(downloadPath);
            await response.Content.CopyToAsync(fileStream);

            // Install the downloaded file
            await InstallUpdateAsync(downloadPath);
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Error", $"Download failed: {ex.Message}", "OK");
        }
    }

    private async Task InstallUpdateAsync(string filePath)
    {
#if WINDOWS
            await AppShell.DisplayException("Installation rigth now not supported on this platform.");
            // For Windows, open the MSIX file
            //await Launcher.OpenAsync(new OpenFileRequest { File = new ReadOnlyFile(filePath) });
#elif ANDROID
            // For Android, create intent to install APK
            var context = Android.App.Application.Context;
            var intent = new Android.Content.Intent(Android.Content.Intent.ActionView);
            var uri = AndroidX.Core.Content.FileProvider.GetUriForFile(context, $"{context.PackageName}.fileprovider", new Java.IO.File(filePath));
            intent.SetDataAndType(uri, "application/vnd.android.package-archive");
            intent.AddFlags(Android.Content.ActivityFlags.GrantReadUriPermission);
            intent.AddFlags(Android.Content.ActivityFlags.NewTask);
            context.StartActivity(intent);
#else
        await AppShell.DisplayException("Installation not supported on this platform.");
#endif
    }
    
    public async Task UpdateAsync()
    {
        if (!HasNewUpdate) return;
        
        string assetName = GetPlatformAssetName();
        var asset = _latestRelease?.Assets?.FirstOrDefault(a => a.Name == assetName);

        if (asset != null)
            await DownloadAndInstallAsync(asset.BrowserDownloadUrl, assetName);
        else
            await AppShell.DisplayException("No suitable asset found for this platform.");
    }

    public async Task OnInternetConnectedAsync()
    {
        bool newValue = await CheckForUpdatesAsync();
        if (newValue != HasNewUpdate)
        {
            HasNewUpdate = newValue;
            UpdateAvailabilityChanged?.Invoke(newValue);
        }
    }

    public async Task OnInternetDisconnectedAsync()
    {
        HasNewUpdate = false;
        UpdateAvailabilityChanged?.Invoke(false);
    }
}

// JSON models for GitHub API response
public class GitHubRelease
{
    [JsonPropertyName("tag_name")]
    public string TagName { get; set; }

    [JsonPropertyName("assets")]
    public GitHubAsset[] Assets { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("body")]
    public string Body { get; set; }

    [JsonPropertyName("published_at")]
    public DateTime? PublishedAt { get; set; }
}

public class GitHubAsset
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("browser_download_url")]
    public string BrowserDownloadUrl { get; set; }
    
    [JsonPropertyName("content_type")]
    public string ContentType { get; set; }

    [JsonPropertyName("size")]
    public long Size { get; set; }
}