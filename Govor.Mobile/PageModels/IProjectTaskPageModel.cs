using CommunityToolkit.Mvvm.Input;
using Govor.Mobile.Models;

namespace Govor.Mobile.PageModels
{
    public interface IProjectTaskPageModel
    {
        IAsyncRelayCommand<ProjectTask> NavigateToTaskCommand { get; }
        bool IsBusy { get; }
    }
}