using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Xml.Linq;

namespace Govor.Mobile.PageModels;

[QueryProperty(nameof(Url), "Url")]
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

    [ObservableProperty]
    private bool canGoBack;

    [ObservableProperty]
    private bool canGoForward;

    public const string HomePage = "https://www.google.com/";

    private WeakReference<WebView> _webViewRef;

    // Метод для установки WebView из кода
    public void SetWebView(WebView webView)
    {
        _webViewRef = new WeakReference<WebView>(webView);
        UpdateNavigationState();
    }

    public void UpdateNavigationState()
    {
        if (_webViewRef.TryGetTarget(out var webView))
        {
            CanGoBack = webView.CanGoBack;
            CanGoForward = webView.CanGoForward;
        }
        else
        {
            CanGoBack = false;
            CanGoForward = false;
        }
    }

    [RelayCommand]
    private void ToggleSearch()
    {
        IsSearchVisible = !IsSearchVisible;
    }

    [RelayCommand]
    private void Refresh()
    {
        if (_webViewRef?.TryGetTarget(out var webView) == true)
        {
            webView.Reload();
        }
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

        UpdateNavigationState();
    }

    [RelayCommand]
    private async Task GoBackAsync()
    {
        if (_webViewRef?.TryGetTarget(out var webView) == true && webView.CanGoBack)
        {
            webView.GoBack();
            await Task.Delay(100); // Даем время на обновление
            UpdateNavigationState();
        }
    }

    [RelayCommand]
    private async Task GoForwardAsync()
    {
        if (_webViewRef?.TryGetTarget(out var webView) == true && webView.CanGoForward)
        {
            webView.GoForward();
            await Task.Delay(100);
            UpdateNavigationState();
        }
    }

    [RelayCommand]
    private async Task GoHomeAsync()
    {
        CurrentUrl = "";
        CurrentUrl = HomePage;
        IsSearchVisible = false;
        await Task.Delay(300);
        UpdateNavigationState();
    }
}
