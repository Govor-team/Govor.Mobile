using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Govor.Mobile.Models.Groups;
using Govor.Mobile.Services.Api;
using Govor.Mobile.Services.Interfaces;
using Govor.Mobile.Utilities;

namespace Govor.Mobile.PageModels.MainFlow.Groups;

public partial class GroupsExploreModel(GroupsApiService api, IServerIpProvider server) : GroupScreenModel
{
    public ObservableCollection<GroupProfile> Results { get; } = new();
    [ObservableProperty] private string query = "";
    [ObservableProperty] private string invitationInput = "";
    [ObservableProperty] private GroupProfile? invitationPreview;
    [ObservableProperty] private string createName = "";
    [ObservableProperty] private string createDescription = "";
    [ObservableProperty] private bool createPrivate = true;
    [ObservableProperty] private bool createChannel;
    [ObservableProperty] private int section;
    [ObservableProperty] private bool hasMore;
    public bool IsSearch => Section == 0;
    public bool IsInvite => Section == 1;
    public bool IsCreate => Section == 2;
    public bool HasPreview => InvitationPreview != null;
    private string? _previewCode;
    private string _searchQuery = "";
    private int _skip;
    partial void OnSectionChanged(int value) { OnPropertyChanged(nameof(IsSearch)); OnPropertyChanged(nameof(IsInvite)); OnPropertyChanged(nameof(IsCreate)); }
    partial void OnInvitationInputChanged(string value) { _previewCode = null; InvitationPreview = null; }
    partial void OnInvitationPreviewChanged(GroupProfile? value) => OnPropertyChanged(nameof(HasPreview));
    [RelayCommand] private void ShowSearch() => Section = 0;
    [RelayCommand] private void ShowInvite() => Section = 1;
    [RelayCommand] private void ShowCreate() => Section = 2;
    [RelayCommand] private Task Search() => RunAsync(async () =>
    {
        if (Query.Trim().Length > 100) throw new ArgumentException("Поиск: максимум 100 символов.");
        _searchQuery = Query.Trim();
        var batch = await api.SearchAsync(_searchQuery);
        Results.Clear(); foreach (var group in batch) Results.Add(group);
        _skip = batch.Count; HasMore = batch.Count == 50;
        if (Results.Count == 0) Status = "Публичные сообщества не найдены.";
    });
    [RelayCommand] private Task More() => RunAsync(async () =>
    {
        var batch = await api.SearchAsync(_searchQuery, _skip); _skip += batch.Count;
        foreach (var group in batch.Where(g => Results.All(x => x.Id != g.Id))) Results.Add(group);
        HasMore = batch.Count == 50;
    });
    [RelayCommand] private Task Open(GroupProfile group) => RunAsync(async () =>
    {
        var current = await api.GetAsync(group.Id);
        if (current.MyRole != null)
            await Shell.Current.GoToAsync($"chat?chatId={current.Id}&isGroup=true", false);
        else await OpenProfileAsync(current.Id);
    });
    [RelayCommand] private Task PreviewInvite() => RunAsync(async () =>
    {
        var code = GroupInviteParser.Parse(InvitationInput, server.IP);
        var profile = await api.PreviewInviteAsync(code);
        // A changed entry must not join a previously previewed code.
        if (code != GroupInviteParser.Parse(InvitationInput, server.IP)) return;
        _previewCode = code; InvitationPreview = profile;
    });
    [RelayCommand] private Task JoinInvite() => RunAsync(async () =>
    {
        if (_previewCode == null || InvitationPreview == null) return;
        var group = await api.JoinInviteAsync(_previewCode);
        await OpenProfileAsync(group.Id);
    });
    [RelayCommand] private Task Create() => RunAsync(async () =>
    {
        if (CreateName.Trim().Length is < 1 or > 100 || CreateDescription.Length > 500)
            throw new ArgumentException("Имя: 1–100 символов; описание: до 500.");
        var group = await api.CreateAsync(CreateName, CreateDescription, CreatePrivate, CreateChannel);
        await OpenProfileAsync(group.Id);
    });
}
