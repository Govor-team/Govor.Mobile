using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Govor.Mobile.Pages.AuthFlow;
using Govor.Mobile.Services.Api;
using Govor.Mobile.Services.Interfaces;

namespace Govor.Mobile.PageModels.AuthFlow;

[QueryProperty(nameof(Name), "Name")]
[QueryProperty(nameof(Password), "Password")]
public partial class LoginPageModel : ObservableObject
{
    private readonly IAuthService _authService;
    private readonly IServiceProvider _serviceProvider;

    [ObservableProperty]
    private string name;

    [ObservableProperty]
    private string password;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private bool isPasswordHidden = true;

    public string EyeIcon => IsPasswordHidden ? "close_eye.png" : "open_eye.png";

    public LoginPageModel(IAuthService authService, IServiceProvider serviceProvider)
    {
        _authService = authService;
        _serviceProvider = serviceProvider;
    }

    [RelayCommand(CanExecute = nameof(CanLogin))]
    private async Task LoginAsync()
    {
        if (IsBusy)
            return;

        IsBusy = true;

        try
        {
            var result = await _authService.LoginAsync(Name.TrimEnd().TrimStart(), Password);

            if (!result.IsSuccess)
            {
                await AppShell.DisplayException(result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            // TODO
            await AppShell.DisplayException("Что-то случилось!");
        }
        finally
        {
            IsBusy = false;
        }

        LoginCommand.NotifyCanExecuteChanged();
    }

    private bool CanLogin()
    {
        return !string.IsNullOrWhiteSpace(Name)
            && !string.IsNullOrWhiteSpace(Password)
            && !IsBusy;
    }

    partial void OnNameChanged(string value) => LoginCommand.NotifyCanExecuteChanged();
    partial void OnPasswordChanged(string value) => LoginCommand.NotifyCanExecuteChanged();
    partial void OnIsBusyChanged(bool value) => LoginCommand.NotifyCanExecuteChanged();

    [RelayCommand]
    private async Task GoToRegisterAsync()
    {
        try
        {
            var navigationParameter = new Dictionary<string, object>
            {
                { "Name", Name },
                { "Password", Password }
            };

            await Shell.Current.GoToAsync(nameof(RegisterPage), true, navigationParameter);
        }
        catch(Exception ex)
        {
            await AppShell.DisplayException($"{ex.Message}");
        }
    }

    [RelayCommand]
    private async Task TogglePasswordVisibilityAsync()
    {
        IsPasswordHidden = !IsPasswordHidden;
    }

    partial void OnIsPasswordHiddenChanged(bool value)
    {
        OnPropertyChanged(nameof(EyeIcon));
    }
}
