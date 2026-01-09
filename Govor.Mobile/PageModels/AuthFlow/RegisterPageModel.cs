using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Govor.Mobile.Pages.AuthFlow;


namespace Govor.Mobile.PageModels.AuthFlow;

[QueryProperty(nameof(Name), "Name")]
[QueryProperty(nameof(Password), "Password")]
public partial class RegisterPageModel : ObservableObject
{
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
    public RegisterPageModel(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    [RelayCommand(CanExecute = nameof(CanNext))]
    private async Task NextAsync()
    {
        if (IsBusy)
            return;

        IsBusy = true;

        var navParams = new Dictionary<string, object>
        {
            { "Name", Name },
            { "Password", Password }
        };

        await Shell.Current.GoToAsync(nameof(CodeInputPage), false, navParams);

        IsBusy = false;

        NextCommand.NotifyCanExecuteChanged();
    }
    partial void OnNameChanged(string value) => NextCommand.NotifyCanExecuteChanged();
    partial void OnPasswordChanged(string value) => NextCommand.NotifyCanExecuteChanged();
    partial void OnIsBusyChanged(bool value) => NextCommand.NotifyCanExecuteChanged();

    private bool CanNext()
    {
        return !string.IsNullOrWhiteSpace(Name)
            && !string.IsNullOrWhiteSpace(Password)
            && !IsBusy;
    }

    [RelayCommand]
    private async Task GoToLoginAsync()
    {
        var navParams = new Dictionary<string, object>
        {
            { "Name", Name },
            { "Password", Password }
        };

        await Shell.Current.GoToAsync("..", true, navParams);
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
