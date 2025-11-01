using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Govor.Mobile.Services.Interfaces;
using System;

namespace Govor.Mobile.PageModels.AuthFlow;

[QueryProperty(nameof(Name), "Name")]
[QueryProperty(nameof(Password), "Password")]
public partial class CodeInputModel : ObservableObject
{
    [ObservableProperty]
    private string name;

    [ObservableProperty]
    private string password;

    [ObservableProperty]
    private string code;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private bool isCodeHidden = true;

    public string EyeIcon => IsCodeHidden ? "close_eye.png" : "open_eye.png";

    private readonly IAuthService _authService;
    private readonly IServiceProvider _serviceProvider;

    public CodeInputModel(IAuthService authService, IServiceProvider serviceProvider)
    {
        _authService = authService;
        _serviceProvider = serviceProvider;
    }

    [RelayCommand]
    private async Task PasswordCompleted()
    {
        if (RegisterCommand.CanExecute(null))
            await RegisterAsync();
    }

    [RelayCommand(CanExecute = nameof(CanNext))]
    private async Task RegisterAsync()
    {
        if (IsBusy)
            return;

        IsBusy = true;

        var result = await _authService.RegisterAsync(Name, Password, Code);
        
        if (!result.IsSuccess)
        {
            await AppShell.DisplaySnackbarAsync(result.ErrorMessage);
        }

        IsBusy = false;

        RegisterCommand.NotifyCanExecuteChanged();
    }

    partial void OnCodeChanged(string value) => RegisterCommand.NotifyCanExecuteChanged();

    private bool CanNext()
    {
        return !string.IsNullOrWhiteSpace(Code)
            && !IsBusy;
    }

    [RelayCommand]
    private async Task OpenWikiAsync()
    {
        var url = "https://nas-3.gitbook.io/govor-api/";
        try
        {
#if ANDROID || IOS

            await Shell.Current.Navigation.PushAsync(new WebBrowserPage(url));
#else
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await Browser.Default.OpenAsync(url, BrowserLaunchMode.External);
            });
#endif
        }
        catch (Exception ex)
        {
            await AppShell.DisplaySnackbarAsync("Не удалось открыть документацию!");
        }
    }

    [RelayCommand]
    private async Task ToggleCodeVisibilityAsync()
    {
        IsCodeHidden = !IsCodeHidden;
    }

    partial void OnIsCodeHiddenChanged(bool value)
    {
        OnPropertyChanged(nameof(EyeIcon));
    }
}
