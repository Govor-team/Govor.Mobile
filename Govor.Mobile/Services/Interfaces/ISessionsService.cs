using Govor.Mobile.Models.Responses;
using System;
using System.Collections.Generic;
using System.Text;

namespace Govor.Mobile.Services.Interfaces
{
    public interface ISessionsService
    {
        Task<Result<List<UserSession>>> GetAllSessionsAsync();
        Task<Result<bool>> CloseSessionAsync(Guid id);
        Task<Result<bool>> CloseCurrentSessionAsync();
        Task<Result<bool>> CloseAllSessionsAsync();
    }
}
