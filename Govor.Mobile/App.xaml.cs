using Govor.Mobile.Pages.AuthFlow;
using Govor.Mobile.Pages.MainFlow;
using Govor.Mobile.Services.Api;
using Govor.Mobile.Services.Interfaces;

namespace Govor.Mobile;

public partial class App : Application
{
    private readonly IAuthService _authService;
    private readonly IServiceProvider _serviceProvider;
    private readonly IAppStartupOrchestrator _initializer;

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

        // Показываем splash / loader
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
        
        _authService.AuthenticationStateChanged += OnAuthenticationStateChanged;

        try
        {
            await _authService.InitializeAsync();
            // иначе ждём события (токен может быть в процессе refresh)
        }
        catch (Exception ex)
        {
            // Логируем + показываем ошибку или дефолтный экран
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await Current.MainPage.DisplayAlertAsync("Ошибка запуска", "Не удалось инициализировать приложение", "OK");
                // или перейти на страницу с retry
            });
        }
    }

    private async void OnAuthenticationStateChanged(object? sender, bool isAuthenticated)
    {
        // Чтобы избежать множественных вызовов
        //_authService.AuthenticationStateChanged -= OnAuthenticationStateChanged;

        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            if (isAuthenticated)
                await NavigateToAuthenticatedAsync();
            else
                MainPage = _serviceProvider.GetRequiredService<AuthShell>();
        });
    }

    private async Task NavigateToAuthenticatedAsync()
    {
        try
        {
            await _initializer.StartAsync();
            MainPage = _serviceProvider.GetRequiredService<MainShell>();
        }
        catch (Exception ex)
        {
            await AppShell.DisplayException("Не удалось загрузить основной интерфейс");
            MainPage = _serviceProvider.GetRequiredService<AuthShell>();
        }
    }

    protected override void OnSleep()
    {
        // можно отписаться, если нужно
        // _authService.AuthenticationStateChanged -= OnAuthenticationStateChanged;
    }
}