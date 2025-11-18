namespace Govor.Mobile.Pages;

public partial class WikiPage : ContentPage
{
    private readonly string _url;

    public WikiPage(string url)
    {
        InitializeComponent();
        _url = url;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        WikiWebView.Source = _url;
    }
}