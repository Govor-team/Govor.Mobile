using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Govor.Mobile.Models.Reactions;
using Govor.Mobile.PageModels.ContentViewsModel.Reactions;
using Govor.Mobile.Services.Implementations;

namespace Govor.Mobile.PageModels.MainFlow;

public sealed class ReactionPackItemViewModel(ReactionPack pack, bool installed)
{
    public ReactionPack Pack { get; } = pack;
    public string Name => Pack.Name;
    public string Description => Pack.Description;
    public bool IsDefault => Pack.IsDefault;
    public bool CanToggle => !IsDefault;
    public bool IsInstalled { get; } = installed;
    public string ActionText => IsInstalled ? "Убрать из моих паков" : "Добавить пак";
    public string Badge => IsDefault ? "Всегда доступен" : IsInstalled ? "Добавлен" : "";
    public List<ReactionChoiceViewModel> Preview { get; } = pack.Reactions
        .Where(r => r.IsEnabled).Take(18).Select(r => new ReactionChoiceViewModel(r)).ToList();
}

public partial class ReactionPacksPageModel(ReactionService service) : ObservableObject
{
    private List<ReactionPack> _mine = new();
    private List<ReactionPack> _catalog = new();
    public ObservableCollection<ReactionPackItemViewModel> Packs { get; } = new();

    [ObservableProperty] private bool isCatalog;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string status = "";
    [ObservableProperty] private string searchText = "";
    [ObservableProperty] private string shareCode = "";

    public bool HasStatus => !string.IsNullOrWhiteSpace(Status);
    partial void OnStatusChanged(string value) => OnPropertyChanged(nameof(HasStatus));

    public bool IsEmpty => Packs.Count == 0 && !IsBusy;
    partial void OnSearchTextChanged(string value) => UpdateVisiblePacks();
    partial void OnIsBusyChanged(bool value) => OnPropertyChanged(nameof(IsEmpty));

    public async Task LoadAsync(bool force = false)
    {
        if (IsBusy)
            return;

        IsBusy = true;
        Status = "";
        try
        {
            _mine = await service.GetPacksAsync(false, force);
            if (IsCatalog)
                _catalog = await service.GetPacksAsync(true, force);

            Status = service.CatalogWarning ?? "";
            UpdateVisiblePacks();
        }
        catch (Exception ex)
        {
            Status = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void ShowMine()
    {
        IsCatalog = false;
        UpdateVisiblePacks();
    }

    [RelayCommand]
    private async Task ShowCatalogAsync()
    {
        IsCatalog = true;
        await LoadAsync();
    }

    [RelayCommand]
    private Task RefreshAsync() => LoadAsync(force: true);

    [RelayCommand]
    private async Task OpenSharedAsync()
    {
        if (IsBusy || string.IsNullOrWhiteSpace(ShareCode))
            return;

        IsBusy = true;
        try
        {
            var pack = await service.GetSharedAsync(ShareCode);
            _catalog = new() { pack };
            IsCatalog = true;
            SearchText = "";
            Status = "";
            UpdateVisiblePacks();
        }
        catch (Exception ex)
        {
            Status = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task TogglePackAsync(ReactionPackItemViewModel item)
    {
        if (IsBusy || item.IsDefault)
            return;

        IsBusy = true;
        try
        {
            await service.SubscribeAsync(item.Pack, !item.IsInstalled);
            _mine = await service.GetPacksAsync();
            Status = item.IsInstalled ? "Пак убран из выбора" : "Пак добавлен";
            UpdateVisiblePacks();
        }
        catch (Exception ex)
        {
            Status = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task CopyCodeAsync(ReactionPackItemViewModel item)
    {
        await Clipboard.Default.SetTextAsync(item.Pack.ShareCode);
        Status = "Код пака скопирован";
    }

    private void UpdateVisiblePacks()
    {
        var source = IsCatalog ? _catalog : _mine;
        var filtered = source.Where(p => string.IsNullOrWhiteSpace(SearchText)
            || p.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

        Packs.Clear();
        foreach (var pack in filtered)
        {
            var item = new ReactionPackItemViewModel(pack, _mine.Any(p => p.Id == pack.Id));
            Packs.Add(item);
            foreach (var preview in item.Preview)
                _ = preview.LoadMediaAsync(service);
        }
        OnPropertyChanged(nameof(IsEmpty));
    }
}
