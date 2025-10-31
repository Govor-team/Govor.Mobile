namespace Govor.Mobile.Pages;

public partial class WebBrowserPage : ContentPage
{
    public WebBrowserPage(string url)
    {
        InitializeComponent();

        var vm = new WebBrowserPageModel();
        BindingContext = vm;

        // подписка на события WebView
        Web.Navigated += vm.OnNavigated;

        vm.CurrentUrl = url;
    }

    private void OnUrlEntered(object sender, EventArgs e)
    {
        if (BindingContext is WebBrowserPageModel vm)
        {
            vm.OnUrlEntered();
        }
    }
}