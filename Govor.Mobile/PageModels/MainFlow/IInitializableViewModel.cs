namespace Govor.Mobile.PageModels.MainFlow;

public interface IInitializableViewModel
{
    public bool IsLoaded { get; protected set; }
    Task InitAsync();
}
