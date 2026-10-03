using Govor.Mobile.Services;
using Govor.Mobile.Services.Interfaces;
using Microsoft.Maui.Controls;

namespace Govor.Mobile;

public partial class App : Application
{
    private readonly IAppShellCoordinator _shellCoordinator;
    private Window? _window;
    private readonly NetworkAvailabilityService _network;

    public App(
        IBackgroundImageService backgroundService,
        IAppShellCoordinator shellCoordinator,
        NetworkAvailabilityService network)
    {
        InitializeComponent();

        _shellCoordinator = shellCoordinator;
        _network = network;
        _shellCoordinator.RootPageChanged += OnRootPageChanged;

        backgroundService.LoadCurrent();

        MainPage = new ContentPage
        {
            Content = new ActivityIndicator
            {
                IsRunning = true,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            },
            BackgroundColor = Color.FromArgb("#282A37")
        };
    }

    protected override async void OnStart()
    {
        base.OnStart();

        try
        {
            await _shellCoordinator.InitializeAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"APP INIT ERROR: {ex}");
            var retry = new Button { Text = "Повторить запуск" };
            retry.Clicked += async (_, _) =>
            {
                retry.IsEnabled = false;
                try { await _shellCoordinator.InitializeAsync(); }
                finally { retry.IsEnabled = true; }
            };
            OnRootPageChanged(this, new ContentPage
            {
                Content = new VerticalStackLayout
                {
                    Padding = 24,
                    VerticalOptions = LayoutOptions.Center,
                    Children = { new Label { Text = "Не удалось подготовить приложение. Повторите запуск." }, retry }
                }
            });
        }
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        _window = new Window(MainPage);
        return _window;
    }

    private void OnRootPageChanged(object? sender, Page page)
    {
        MainPage = page;

        if (_window is not null && _window.Page != page)
            _window.Page = page;
    }

    protected override void OnSleep()
    {
        base.OnSleep();
    }

    protected override void OnResume()
    {
        base.OnResume();
        _ = _network.CheckInitialConnectivityAsync();
    }
}
