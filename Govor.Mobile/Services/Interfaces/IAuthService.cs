using Govor.Mobile.Models.Responses;
using Govor.Mobile.Models.Results;
using System;
using System.Collections.Generic;
using System.Text;

namespace Govor.Mobile.Services.Interfaces
{
    public interface IAuthService
    {
        bool IsAuthenticated { get; }

        event EventHandler<bool> AuthenticationStateChanged;
        Task<Result<UserLogin>> LoginAsync(string username, string password);
        Task<Result<UserLogin>> RegisterAsync(string username, string password, string invitation);
        Task InitializeAsync();
        Task LogoutAsync();
    }
}
