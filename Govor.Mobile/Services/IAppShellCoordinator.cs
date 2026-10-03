using Microsoft.Maui.Controls;

namespace Govor.Mobile.Services;

public interface IAppShellCoordinator
{
    event EventHandler<Page>? RootPageChanged;

    Task InitializeAsync();
}
