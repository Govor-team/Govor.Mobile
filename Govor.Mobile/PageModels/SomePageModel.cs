using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Govor.Mobile.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Govor.Mobile.PageModels
{
    public partial class SomePageModel : ObservableObject
    {
        private readonly IAuthService _authService;
        public SomePageModel(IAuthService authService)
        {
            _authService = authService;
        }

        [RelayCommand]
        private async Task LogoutAsync()
        {
            await _authService.LogoutAsync();
        }
    }
}
