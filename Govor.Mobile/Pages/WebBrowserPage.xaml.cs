using Microsoft.Maui.Platform;

namespace Govor.Mobile.Pages;


public partial class WebBrowserPage : ContentPage
{
    public WebBrowserPage(string url)
    {
        InitializeComponent();

        var vm = BindingContext as WebBrowserPageModel;
        vm.CurrentUrl = url;
        // Передаём WebView в ViewModel
        vm.SetWebView(Web);

        // подписка на события WebView
        Web.Navigated += vm.OnNavigated;
        Web.Navigating += (s, e) => vm.UpdateNavigationState();
    }

    private void OnUrlEntered(object sender, EventArgs e)
    {
        if (BindingContext is WebBrowserPageModel vm)
        {
            vm.OnUrlEntered();
        }
    }
}