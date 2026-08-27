using Govor.Mobile.Pages.AuthFlow;
using Govor.Mobile.Pages.MainFlow;
using Govor.Mobile.Services.Api;
using Govor.Mobile.Services.Interfaces;
using Govor.Mobile.Services.Interfaces.Notification;
using Microsoft.Maui.Controls;

namespace Govor.Mobile;

public partial class App : Application
{
    private readonly IAuthService _authService;
    private readonly IServiceProvider _serviceProvider;
    private readonly IAppStartupOrchestrator _initializer;

    private readonly SemaphoreSlim _stateSemaphore = new(1, 1);

    private bool _isInitialized;
    private bool? _requestedAuthenticationState;

    public App(
        IAuthService authService,
        IServiceProvider serviceProvider,
        IBackgroundImageService backgroundService,
        IAppStartupOrchestrator startupOrchestrator)
    {
        InitializeComponent();

        _authService = authService;
        _serviceProvider = serviceProvider;
        _initializer = startupOrchestrator;

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

        if (_isInitialized)
            return;

        _isInitialized = true;

        _authService.AuthenticationStateChanged +=
            OnAuthenticationStateChanged;

        try
        {
            Console.WriteLine("AUTH: Initialize START");

            await _authService.InitializeAsync();

            Console.WriteLine(
                $"AUTH: Initialize END = {_authService.IsAuthenticated}");

            SetAuthenticationState(_authService.IsAuthenticated);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"AUTH INIT ERROR: {ex}");

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await AppShell.DisplayException(
                    "Не удалось инициализировать приложение");
            });
        }
    }

    private void OnAuthenticationStateChanged(
        object? sender,
        bool isAuthenticated)
    {
        Console.WriteLine(
            $"AUTH EVENT: {isAuthenticated}");

        SetAuthenticationState(isAuthenticated);
    }

    private void SetAuthenticationState(bool isAuthenticated)
    {
        _requestedAuthenticationState = isAuthenticated;

        _ = ApplyAuthenticationStateAsync();
    }

    private async Task ApplyAuthenticationStateAsync()
    {
        // Если другой transition уже выполняется,
        // он после завершения увидит актуальное состояние.
        if (!await _stateSemaphore.WaitAsync(0))
            return;

        try
        {
            while (true)
            {
                var targetState = _requestedAuthenticationState;

                if (targetState is null)
                    return;

                var isAuthenticated = targetState.Value;

                if (isAuthenticated)
                {
                    if (MainPage is MainShell)
                        return;

                    Console.WriteLine("AUTH: Switching -> MainShell");

                    await NavigateToAuthenticatedAsync();
                }
                else
                {
                    if (MainPage is AuthShell)
                        return;

                    Console.WriteLine("AUTH: Switching -> AuthShell");

                    await NavigateToUnauthenticatedAsync();
                }

                // Проверяем, не изменилось ли состояние
                // во время предыдущего transition.
                if (_requestedAuthenticationState == targetState)
                    return;
            }
        }
        finally
        {
            _stateSemaphore.Release();
        }
    }

    private async Task NavigateToAuthenticatedAsync()
    {
        try
        {
            Console.WriteLine("AUTH: Startup orchestrator START");

            await _initializer.StartAsync();

            Console.WriteLine("AUTH: Startup orchestrator END");

            // За время StartAsync пользователь мог выйти.
            if (_requestedAuthenticationState != true)
                return;

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                if (_requestedAuthenticationState != true)
                    return;

                MainPage =
                    _serviceProvider.GetRequiredService<MainShell>();
            });

            Console.WriteLine("AUTH: MainShell assigned");
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"AUTH: MainShell initialization ERROR: {ex}");

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                MainPage =
                    _serviceProvider.GetRequiredService<AuthShell>();
            });
        }
    }

    private async Task NavigateToUnauthenticatedAsync()
    {
        try
        {
            var push =
                _serviceProvider.GetService<IPushNotificationService>();

            if (push != null)
                await push.UnregisterAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"PUSH unregister ERROR: {ex}");
        }

        if (_requestedAuthenticationState != false)
            return;

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            if (_requestedAuthenticationState != false)
                return;

            MainPage =
                _serviceProvider.GetRequiredService<AuthShell>();
        });

        Console.WriteLine("AUTH: AuthShell assigned");
    }

    protected override void OnSleep()
    {
        base.OnSleep();
    }

    protected override void OnResume()
    {
        base.OnResume();
    }
}