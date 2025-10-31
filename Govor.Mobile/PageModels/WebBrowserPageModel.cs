using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Govor.Mobile.PageModels;

public partial class WebBrowserPageModel : ObservableObject
{
    [ObservableProperty]
    private string title = "Документация Govor";

    [ObservableProperty]
    private string shortAddress = "govor.gitbook.io";

    [ObservableProperty]
    private string url = string.Empty;

    [ObservableProperty]
    private string currentUrl = "https://govor.gitbook.io/wiki";

    [ObservableProperty]
    private bool isSearchVisible = false;

    [RelayCommand]
    private void ToggleSearch()
    {
        IsSearchVisible = !IsSearchVisible;
    }

    [RelayCommand]
    private void Refresh()
    {
        CurrentUrl = CurrentUrl;
    }

    public void OnUrlEntered()
    {
        if (string.IsNullOrWhiteSpace(Url))
            return;

        var text = Url.Trim();

        if (!text.StartsWith("http"))
        {
            // Если это просто текст — ищем в Google
            text = $"https://www.google.com/search?q={Uri.EscapeDataString(text)}";
        }
        else if (!text.StartsWith("http://") && !text.StartsWith("https://"))
        {
            text = "https://" + text;
        }

        CurrentUrl = text;
        IsSearchVisible = false;
    }

    public void OnNavigated(object sender, WebNavigatedEventArgs e)
    {
        if (Uri.TryCreate(e.Url, UriKind.Absolute, out var uri))
        {
            ShortAddress = uri.Host;
        }

        Title = e.Url switch
        {
            string url when url.Contains("govor-api/") => "Документация Govor",
            _ => "Браузер"
        };
    }
}
