namespace Govor.Mobile.Services.Api.Base;

public static class HttpRequestOptionsKeys
{
    public static readonly HttpRequestOptionsKey<bool> RequireAuth =
        new HttpRequestOptionsKey<bool>("RequireAuth");
}