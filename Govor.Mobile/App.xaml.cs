using Govor.Mobile.Pages.AuthFlow;
using Govor.Mobile.Pages.MainFlow;
using Govor.Mobile.Services.Api;

namespace Govor.Mobile
{
    public partial class App : Application
    {
        private readonly IAuthService _authService;
        private readonly IServiceProvider _serviceProvider;

        public App(IAuthService authService, IServiceProvider serviceProvider)
        {
            InitializeComponent();

            _authService = authService;
            _serviceProvider = serviceProvider;

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

            InitializeAsync();
        }

        private async void InitializeAsync()
        {
            await _authService.InitializeAsync();
            _authService.AuthenticationStateChanged += OnAuthenticationStateChanged;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                
                MainPage = _authService.IsAuthenticated
                     ? _serviceProvider.GetRequiredService<MainShell>()
                     : _serviceProvider.GetRequiredService<AuthShell>();
                
                
                //MainPage = _serviceProvider.GetRequiredService<MainShell>();
            });
        }


        private void OnAuthenticationStateChanged(object? sender, bool isAuthenticated)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (isAuthenticated)
                {
                    MainPage = _serviceProvider.GetRequiredService<MainShell>();
                }
                else
                {
                    MainPage = _serviceProvider.GetRequiredService<AuthShell>();
                    // = _serviceProvider.GetRequiredService<MainShell>();
                }
            });
        }
    }
}