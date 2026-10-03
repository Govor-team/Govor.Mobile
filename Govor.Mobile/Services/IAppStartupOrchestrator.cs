namespace Govor.Mobile.Services;

public interface IAppStartupOrchestrator
{
    Task InitializeLocalAsync();
    Task StartAsync();
}
