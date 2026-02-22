using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Govor.Mobile.Services.Interfaces;

namespace Govor.Mobile.PageModels.MainFlow;

public partial class RootPageViewModel : ObservableObject
{
    // Индекс выбранной вкладки (связан с ViewSwitcher и TabHostView)
    [ObservableProperty]
    private int _selectedViewModelIndex = -1;
    
    [ObservableProperty]
    private bool hasNewUpdate;
    
    [ObservableProperty]
    private bool isSettingsOpen = false;
    public MainPageModel HomePageViewModel { get; }
    public FriendsSearchPageModel FriendsPageViewModel { get; }
    
    public SettingsPageModel SettingsViewModel { get; }
    // public CallViewModel CallPageViewModel { get; }
    
    private readonly IUpdateService _updateService;

    public RootPageViewModel(
        MainPageModel homeVM, 
        FriendsSearchPageModel friendsVM,
        IUpdateService updateService)
    {
        _updateService =  updateService;
        HomePageViewModel = homeVM;
        FriendsPageViewModel = friendsVM;
        //CallPageViewModel = callVM;

        // Устанавливаем начальную вкладку
        SelectedViewModelIndex = 0;
        
        updateService.UpdateAvailabilityChanged += value => HasNewUpdate = value;
        HasNewUpdate = updateService.HasNewUpdate;
    }

    partial void OnSelectedViewModelIndexChanged(int value)
    {
        if (value < 0) return;

        // Используем MainThread для UI-операций и Task.Run для логики
        Task.Run(async () => await InitializeTab(value));
    }

    private async Task InitializeTab(int index)
    {
        // Отладочный вывод, чтобы вы видели в консоли, вызывается ли метод
        System.Diagnostics.Debug.WriteLine($"---> Переключение на вкладку: {index}");

        IInitializableViewModel? viewModelToInit = index switch
        {
            0 => HomePageViewModel,
            1 => FriendsPageViewModel,
            _ => null
        };

        if (viewModelToInit != null && !viewModelToInit.IsLoaded)
        {
            await viewModelToInit.InitAsync();
        }
    }
    
    [RelayCommand]
    private void DownloadUpdate()
    {
        if (HasNewUpdate)
           _ = _updateService.UpdateAsync();
    }
}