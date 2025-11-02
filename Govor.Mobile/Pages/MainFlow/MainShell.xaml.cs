namespace Govor.Mobile.Pages.MainFlow;

public partial class MainShell : Shell
{
    public MainShell()
    {
        InitializeComponent();

        Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));
    }
}
