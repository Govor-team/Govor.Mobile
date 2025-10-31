using Govor.Mobile.Pages.AuthFlow;
using Govor.Mobile.Services.Interfaces;

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

            _authService.AuthenticationStateChanged += OnAuthenticationStateChanged;

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
                    ? _serviceProvider.GetRequiredService<SomePage>()
                    : _serviceProvider.GetRequiredService<AuthShell>();
            });
        }


        private void OnAuthenticationStateChanged(object? sender, bool isAuthenticated)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (isAuthenticated)
                {
                    MainPage = _serviceProvider.GetRequiredService<SomePage>();
                }
                else
                {
                    MainPage = _serviceProvider.GetRequiredService<LoginPage>();
                }
            });
        }
    }
}