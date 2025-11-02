namespace Govor.Mobile.Pages;

public partial class BrowserBottomSheet : ContentView
{
    public BrowserBottomSheet()
    {
        InitializeComponent();

        if (BindingContext is WebBrowserPageModel vm)
        {
            vm.SetWebView(Web);
            Web.Navigated += vm.OnNavigated;
            Web.Navigating += (s, e) => vm.UpdateNavigationState();
        }
    }

    public void Open(string url)
    {
        if (BindingContext is WebBrowserPageModel vm)
        {
            vm.CurrentUrl = url;
            vm.SetWebView(Web);
        }
    }

    private void OnUrlEntered(object sender, EventArgs e)
    {
        if (BindingContext is WebBrowserPageModel vm)
            vm.OnUrlEntered();
    }
}