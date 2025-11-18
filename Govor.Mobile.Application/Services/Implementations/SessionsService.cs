using Govor.Mobile.Application.Models.Responses;
using Govor.Mobile.Application.Models.Results;
using Govor.Mobile.Application.Services.Api.Base;
using Govor.Mobile.Application.Services.Interfaces;

namespace Govor.Mobile.Application.Services.Implementations;

public class SessionsService : ISessionsService
{
    private readonly IApiClient _apiClient;

    public SessionsService(IApiClient apiClient)
    {
        _apiClient = apiClient; 
    }

    public async Task<Result<bool>> CloseAllSessionsAsync()
    {
        var result = await _apiClient.DeleteAsync("api/session/close/all");

        if (result.IsSuccess)
        {
            return Result<bool>.Success(true);
        }
        else
        {
            return Result<bool>.Failure(result.ErrorMessage);
        }
    }

    public async Task<Result<bool>> CloseCurrentSessionAsync()
    {
        var result = await _apiClient.DeleteAsync($"api/session/close/");

        if (result.IsSuccess)
        {
            return Result<bool>.Success(true);
        }
        else
        {
            return Result<bool>.Failure(result.ErrorMessage);
        }
    }

    public async Task<Result<bool>> CloseSessionAsync(Guid id)
    {
        var result = await _apiClient.DeleteAsync($"api/session/close/{id}");

        if (result.IsSuccess)
        {
            return Result<bool>.Success(true);
        }
        else
        {
            return Result<bool>.Failure(result.ErrorMessage);
        }
    }

    public async Task<Result<List<UserSession>>> GetAllSessionsAsync()
    {
        var result = await _apiClient.GetAsync<List<UserSession>>("api/session/all");

        if (result.IsSuccess)
        {
            return Result<List<UserSession>>.Success(result.Value);
        }
        else
        {
            return Result<List<UserSession>>.Failure(result.ErrorMessage);
        }
    }
}
