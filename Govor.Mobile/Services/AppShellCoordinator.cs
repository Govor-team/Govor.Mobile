using Govor.Mobile.Pages.AuthFlow;
using Govor.Mobile.Pages.MainFlow;
using Govor.Mobile.Services.Api;
using Govor.Mobile.Services.Interfaces.Notification;
using Microsoft.Extensions.Logging;
using Govor.Mobile.Services.Hubs;

namespace Govor.Mobile.Services;

public sealed class AppShellCoordinator : IAppShellCoordinator
{
    private readonly IAuthService _authService;
    private readonly IAppStartupOrchestrator _startupOrchestrator;
    private readonly IServiceProvider _serviceProvider;
    private readonly IPushNotificationService _pushNotificationService;
    private readonly ILogger<AppShellCoordinator> _logger;
    private readonly SemaphoreSlim _transitionLock = new(1, 1);

    private bool _isInitialized;
    private bool? _requestedAuthenticationState;
    private Task? _authenticatedStartupTask;
    private bool? _appliedState;
    private readonly IHubInitializer _hubs;

    public AppShellCoordinator(
        IAuthService authService,
        IAppStartupOrchestrator startupOrchestrator,
        IServiceProvider serviceProvider,
        IPushNotificationService pushNotificationService,
        ILogger<AppShellCoordinator> logger,
        IHubInitializer hubs)
    {
        _authService = authService;
        _startupOrchestrator = startupOrchestrator;
        _serviceProvider = serviceProvider;
        _pushNotificationService = pushNotificationService;
        _logger = logger;
        _hubs = hubs;
    }

    public event EventHandler<Page>? RootPageChanged;

    public async Task InitializeAsync()
    {
        if (_isInitialized)
            return;

        _isInitialized = true;
        _authService.AuthenticationStateChanged += OnAuthenticationStateChanged;

        try
        {
            await _authService.InitializeAsync();
            await _startupOrchestrator.InitializeLocalAsync();
            RequestAuthenticationState(_authService.IsAuthenticated);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unable to restore the authentication session.");
            RequestAuthenticationState(false);
        }
    }

    private void OnAuthenticationStateChanged(object? sender, bool isAuthenticated)
    {
        RequestAuthenticationState(isAuthenticated);
    }

    private void RequestAuthenticationState(bool isAuthenticated)
    {
        _requestedAuthenticationState = isAuthenticated;
        _ = ApplyAuthenticationStateAsync();
    }

    private async Task ApplyAuthenticationStateAsync()
    {
        await _transitionLock.WaitAsync();
        try
        {
            while (_requestedAuthenticationState is bool targetState)
            {
                if (_appliedState == targetState) break;
                if (targetState)
                    await ShowAuthenticatedShellAsync();
                else
                    await ShowUnauthenticatedShellAsync();

                _appliedState = targetState;

                if (_requestedAuthenticationState == targetState)
                    break;
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unable to change the application authentication state.");
        }
        finally
        {
            _transitionLock.Release();
        }
    }

    private async Task ShowAuthenticatedShellAsync()
    {
        await _startupOrchestrator.InitializeLocalAsync();
        var shell = await MainThread.InvokeOnMainThreadAsync(
            () => _serviceProvider.GetRequiredService<MainShell>());

        if (_requestedAuthenticationState != true)
            return;

        await MainThread.InvokeOnMainThreadAsync(() => PublishRootPage(shell));

        await RunAuthenticatedStartupAsync();
    }

    private async Task ShowUnauthenticatedShellAsync()
    {
        await _hubs.DisconnectAllAsync();
        var shell = await MainThread.InvokeOnMainThreadAsync(
            () => _serviceProvider.GetRequiredService<AuthShell>());

        if (_requestedAuthenticationState != false)
            return;

        await MainThread.InvokeOnMainThreadAsync(() => PublishRootPage(shell));
        _ = UnregisterPushNotificationsAsync();
    }

    private void PublishRootPage(Page page)
    {
        RootPageChanged?.Invoke(this, page);
    }

    private async Task RunAuthenticatedStartupAsync()
    {
        try
        {
            await _startupOrchestrator.StartAsync();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Authenticated application startup failed.");
        }
    }

    private async Task UnregisterPushNotificationsAsync()
    {
        try
        {
            await _pushNotificationService.UnregisterAsync();
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Unable to unregister push notifications.");
        }
    }
}
