using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Govor.Mobile.Services.Interfaces;
using System.Diagnostics;

namespace Govor.Mobile.PageModels.MainFlow;

public partial class RootPageViewModel : ObservableObject
{
    private readonly IUpdateService _updateService;

    private readonly SemaphoreSlim _initializationLock = new(1, 1);

    [ObservableProperty]
    private int selectedViewModelIndex;

    [ObservableProperty]
    private bool hasNewUpdate;


    public MainPageModel HomePageViewModel { get; }
    public FriendsSearchPageModel FriendsPageViewModel { get; }
    public SettingsPageModel? SettingsViewModel { get; }

    public RootPageViewModel(
        MainPageModel homeViewModel,
        FriendsSearchPageModel friendsViewModel,
        IUpdateService updateService,
        SettingsPageModel? settingsViewModel = null)
    {
        HomePageViewModel = homeViewModel;
        FriendsPageViewModel = friendsViewModel;

        SettingsViewModel = settingsViewModel;

        _updateService = updateService;

        SelectedViewModelIndex = 0;

        HasNewUpdate = _updateService.HasNewUpdate;

        _updateService.UpdateAvailabilityChanged += OnUpdateAvailabilityChanged;
    }

    partial void OnSelectedViewModelIndexChanged(int value)
    {
        if (value < 0)
            return;

        _ = InitializeTabAsync(value);
    }

    private async Task InitializeTabAsync(int index)
    {
        IInitializableViewModel? viewModel = index switch
        {
            0 => HomePageViewModel,
            1 => FriendsPageViewModel,
            _ => null
        };

        if (viewModel is null)
            return;

        if (viewModel.IsLoaded)
            return;


        await _initializationLock.WaitAsync();

        try
        {
            if (viewModel.IsLoaded)
                return;

            Debug.WriteLine(
                $"[RootPage] Initializing tab {index}");

            await viewModel.InitAsync();

            Debug.WriteLine(
                $"[RootPage] Tab {index} initialized");
        }
        catch (Exception exception)
        {
            Debug.WriteLine(
                $"[RootPage] Failed to initialize tab {index}: {exception}");
        }
        finally
        {
            _initializationLock.Release();
        }
    }

    private void OnUpdateAvailabilityChanged(bool value)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            HasNewUpdate = value;
        });
    }


    [RelayCommand]
    private async Task DownloadUpdateAsync()
    {
        if (!HasNewUpdate)
            return;

        try
        {
            await _updateService.UpdateAsync();
        }
        catch (Exception exception)
        {
            Debug.WriteLine(
                $"[RootPage] Update failed: {exception}");
        }
    }
}