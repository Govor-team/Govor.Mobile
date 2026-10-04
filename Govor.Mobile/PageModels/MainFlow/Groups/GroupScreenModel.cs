using System.Net;
using CommunityToolkit.Mvvm.ComponentModel;
using Govor.Mobile.Services.Api;

namespace Govor.Mobile.PageModels.MainFlow.Groups;

public abstract partial class GroupScreenModel : ObservableObject
{
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string status = "";
    public bool HasStatus => !string.IsNullOrEmpty(Status);
    partial void OnStatusChanged(string value) => OnPropertyChanged(nameof(HasStatus));

    private readonly SemaphoreSlim _operations = new(1, 1);

    protected async Task RunAsync(Func<Task> operation)
    {
        await _operations.WaitAsync();
        IsBusy = true;
        Status = "";
        try { await operation(); }
        catch (Exception ex)
        {
            Status = ex.Message;
            if (ex is GroupApiException { Status: HttpStatusCode.Forbidden or HttpStatusCode.NotFound })
                await AccessDeniedAsync();
        }
        finally { IsBusy = false; _operations.Release(); }
    }
    protected virtual Task AccessDeniedAsync() => Task.CompletedTask;
    protected static Task<bool> ConfirmAsync(string title, string message) =>
        Shell.Current.CurrentPage.DisplayAlertAsync(title, message, "Подтвердить", "Отмена");
    public static Task OpenProfileAsync(Guid id) => Shell.Current.GoToAsync($"GroupProfilePage?groupId={id}", false);
}
