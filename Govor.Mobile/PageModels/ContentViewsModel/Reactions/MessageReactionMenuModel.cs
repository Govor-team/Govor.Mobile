using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Govor.Mobile.Models.Reactions;
using Govor.Mobile.PageModels.ContentViewsModel.Messages;
using Govor.Mobile.Services.Implementations;

namespace Govor.Mobile.PageModels.ContentViewsModel.Reactions;

public enum MessageMenuAction { Copy, Select, Edit, Delete, OpenPacks }

public partial class ReactionPackTabViewModel(ReactionPack pack) : ObservableObject
{
    public ReactionPack Pack { get; } = pack;
    public string Name => Pack.Name;
    [ObservableProperty] private bool isSelected;
}

public partial class MessageReactionMenuModel : ObservableObject
{
    private readonly ReactionService _service;
    private readonly MessagesViewModel _message;
    private readonly Guid? _groupId;
    private readonly bool _isChannel;
    private ChannelReactionPolicy? _policy;
    private ReactionPackTabViewModel? _selectedPack;
    private List<ReactionChoiceViewModel> _packChoices = new();
    private bool _closed;
    private Guid? _ownReactionId;

    public ObservableCollection<ReactionPackTabViewModel> Packs { get; } = new();
    public ObservableCollection<ReactionChoiceViewModel> Choices { get; } = new();
    public bool CanEdit { get; }
    public bool CanDelete { get; }
    public bool HasMessageActions => CanEdit || CanDelete;
    public bool ShowPackTabs => !_isChannel;
    public double ChoicesHeight => _isChannel
        ? Math.Clamp(Math.Ceiling(Choices.Count / 7d) * 40, 40, 160)
        : 104;
    public event Action? CloseRequested;
    public event Action<MessageMenuAction>? ActionRequested;

    [ObservableProperty] private string searchText = "";
    [ObservableProperty] private string status = "Загружаем реакции…";
    [ObservableProperty] private bool canRemoveReaction;
    [ObservableProperty] private bool isBusy;

    public MessageReactionMenuModel(ReactionService service, MessagesViewModel message, Guid? groupId, bool canEdit, bool canDelete, bool isChannel = false)
    {
        _service = service;
        _message = message;
        _groupId = groupId; CanEdit = canEdit; CanDelete = canDelete;
        _isChannel = isChannel;
    }

    public async Task InitializeAsync()
    {
        try
        {
            await _service.RefreshAsync(_message.Id);
            var packs = await _service.GetPacksAsync(catalog: _isChannel);
            if (_groupId is Guid groupId)
                _policy = await _service.GetPolicyAsync(groupId);

            if (_closed)
                return;

            var state = await _service.GetStateAsync(_message.Id);
            _ownReactionId = state.OwnReactionId;
            CanRemoveReaction = state.OwnReactionId != null || state.OwnReactionCode != null;
            foreach (var pack in packs.Where(p => p.IsEnabled))
                Packs.Add(new ReactionPackTabViewModel(pack));

            if (_isChannel)
                ShowChoices(packs.Where(p => p.IsEnabled).SelectMany(p => p.Reactions));
            else if (Packs.FirstOrDefault() is { } first)
                SelectPack(first);

            Status = _policy?.Mode == 1 ? "Реакции отключены в этом канале"
                : _service.CatalogWarning ?? (_packChoices.Count == 0 ? "Нет доступных реакций" : "Повторное нажатие снимет вашу реакцию");
        }
        catch (Exception ex)
        {
            if (!_closed)
                Status = ex.Message;
        }
    }

    [RelayCommand]
    private void SelectPack(ReactionPackTabViewModel pack)
    {
        if (_selectedPack != null)
            _selectedPack.IsSelected = false;

        _selectedPack = pack;
        pack.IsSelected = true;
        ShowChoices(pack.Pack.Reactions);
    }

    private void ShowChoices(IEnumerable<ReactionDefinition> definitions)
    {
        _packChoices = definitions.Where(r => r.IsEnabled && (_policy?.Allows(r.Id) ?? true)).DistinctBy(r => r.Id)
            .Select(r => new ReactionChoiceViewModel(r) { IsSelected = r.Id == _ownReactionId }).ToList();
        FilterChoices();
        foreach (var choice in _packChoices)
            _ = choice.LoadMediaAsync(_service);
    }

    partial void OnSearchTextChanged(string value) => FilterChoices();

    private void FilterChoices()
    {
        Choices.Clear();
        foreach (var choice in _packChoices.Where(c => string.IsNullOrWhiteSpace(SearchText)
            || (c.Definition.Name + c.Definition.Emoji + c.Definition.Code).Contains(SearchText, StringComparison.OrdinalIgnoreCase)))
            Choices.Add(choice);
        OnPropertyChanged(nameof(ChoicesHeight));
    }

    [RelayCommand]
    private async Task ChooseReactionAsync(ReactionChoiceViewModel choice)
    {
        if (IsBusy)
            return;

        IsBusy = true;
        try
        {
            await _service.ToggleAsync(_message.Id, choice.Definition.Id);
            Close();
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
    private async Task RemoveReactionAsync()
    {
        if (IsBusy)
            return;

        IsBusy = true;
        try
        {
            await _service.RemoveAsync(_message.Id);
            Close();
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
    private void RequestAction(MessageMenuAction action) => ActionRequested?.Invoke(action);

    [RelayCommand]
    public void Close()
    {
        _closed = true;
        CloseRequested?.Invoke();
    }
}
