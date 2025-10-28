using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Govor.Mobile.Pages.Auth_Flow;
using Govor.Mobile.Pages.AuthFlow;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Govor.Mobile.PageModels.AuthFlow
{
    public partial class RegisterPageModel : ObservableObject
    {
        private readonly IServiceProvider _serviceProvider;

        [ObservableProperty]
        private string name;

        [ObservableProperty]
        private string password;

        [ObservableProperty]
        private bool isBusy;

        public RegisterPageModel(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        [RelayCommand]
        private void NameCompleted(Entry entry)
        {
            entry?.FindByName<Entry>("PasswordEntry")?.Focus();
        }

        [RelayCommand]
        private async Task PasswordCompleted()
        {
            if (NextCommand.CanExecute(null))
                await NextAsync();
        }

        [RelayCommand(CanExecute = nameof(CanNext))]
        private async Task NextAsync()
        {
            if (IsBusy)
                return;

            IsBusy = true;

            var registerPage = _serviceProvider.GetRequiredService<CodeInputPage>();

            if (registerPage.BindingContext is CodeInputModel vm)
            {
                vm.SetData(Name, Password);
            }

            Application.Current.MainPage = registerPage;

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
            var loginPage = _serviceProvider.GetRequiredService<LoginPage>();
            
            if (loginPage.BindingContext is LoginPageModel vm)
            {
                vm.Name = Name;
                vm.Password = Password;
            }

            Application.Current.MainPage = loginPage;
        }
    }
}
