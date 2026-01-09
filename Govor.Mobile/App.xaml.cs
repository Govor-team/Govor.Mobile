using Govor.Mobile.Pages.AuthFlow;
using Govor.Mobile.Pages.MainFlow;
using Govor.Mobile.Services.Api;
using Govor.Mobile.Services.Hubs;
using Govor.Mobile.Services.Interfaces;

namespace Govor.Mobile
{
    public partial class App : Application
    {
        private readonly IAuthService _authService;
        private readonly IServiceProvider _serviceProvider;
        private readonly IHubInitializer _initializer;
        public App(IAuthService authService, IServiceProvider serviceProvider, IHubInitializer initializer, IBackgroundImageService backgroundService)
        {
            InitializeComponent();
            
            _authService = authService;
            _serviceProvider = serviceProvider;
            _initializer = initializer; 
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
            _authService.AuthenticationStateChanged += OnAuthenticationStateChanged;
            await _authService.InitializeAsync();
        }
        
        private void OnAuthenticationStateChanged(object? sender, bool isAuthenticated)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (isAuthenticated)
                { 
                    _initializer.ConnectAllAsync();
                    MainPage = _serviceProvider.GetRequiredService<MainShell>();
                    //MainPage = _serviceProvider.GetRequiredService<RootMainPage>();
                }
                else
                {
                    _initializer.DisconnectAllAsync();
                    MainPage = _serviceProvider.GetRequiredService<AuthShell>();
                    // = _serviceProvider.GetRequiredService<MainShell>();
                }
            });
        }
        
    }
}