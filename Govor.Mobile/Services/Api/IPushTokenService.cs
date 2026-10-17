namespace Govor.Mobile.Services.Api;

public interface IPushTokenService
{
    Task<Result<bool>> PushToken(string token, string platform);
    Task<Result<bool>> DeleteTokenAsync();
}
