using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Govor.Mobile.Models.Groups;
using Govor.Mobile.Models.Responses;
using Govor.Mobile.PageModels.ContentViewsModel;
using Govor.Mobile.Services.Api;
using Govor.Mobile.Services.Implementations;
using Govor.Mobile.Services.Interfaces;
using Govor.Mobile.Services.Interfaces.JwtServices;
using Govor.Mobile.Services.Interfaces.Repositories;

namespace Govor.Mobile.PageModels.MainFlow.Groups;

public sealed class GroupListItem(GroupProfile group, UserListItemViewModel row)
{
    public GroupProfile Group { get; set; } = group;
    public UserListItemViewModel Row { get; } = row;
    public DateTime LastActivity { get; set; }
}

public partial class GroupsHomeModel : GroupScreenModel
{
    private readonly GroupsApiService _api;
    private readonly LocalAccountCache _cache;
    private readonly IMessagesRepository _messages;
    private readonly GroupRowFactory _rows;
    private readonly IJwtProviderService _session;
    private readonly IDisposable _updates;
    private readonly IUnreadMessagesService _unread;
    public ObservableCollection<GroupListItem> Groups { get; } = new();
    [ObservableProperty] private bool hasMore;
    private int _skip;

    public GroupsHomeModel(GroupsApiService api, LocalAccountCache cache, IMessagesRepository messages,
        IJwtProviderService session, GroupRowFactory rows, GroupRealtimeService realtime, IUnreadMessagesService unread)
    {
        _api = api; _cache = cache; _messages = messages; _rows = rows; _session = session;
        _unread = unread;
        unread.UnreadCountChanged += OnUnreadCountChanged;
        messages.OnNewMessage += message => MainThread.BeginInvokeOnMainThread(() => UpdateActivity(message));
        session.WasClearTokens += () => MainThread.BeginInvokeOnMainThread(() => { Groups.Clear(); _skip = 0; });
        _updates = realtime.Subscribe(null, reconnect => RefreshAsync(reconnect));
    }

    public Task RefreshAsync(bool reconnect = false) => RunAsync(async () =>
    {
        if (_session.CurrentUserId is not Guid user) return;
        var saved = await _cache.ReadAsync<List<GroupProfile>>("my-groups");
        if (_session.CurrentUserId != user) return;
        if (Groups.Count == 0 && saved != null)
        {
            Apply(saved, false);
            foreach (var item in Groups.ToArray())
            {
                var cachedUnread = await _unread.GetCachedCountAsync(item.Group.Id, isGroup: true);
                if (_session.CurrentUserId != user) return;
                item.Row.UnreadCount = cachedUnread;
            }
        }
        // Preserve loaded pages across realtime updates, including removals that shift pagination.
        var count = Math.Max(50, _skip);
        var groups = new List<GroupProfile>();
        List<GroupProfile> batch;
        do
        {
            batch = await _api.MineAsync(groups.Count);
            groups.AddRange(batch);
        } while (batch.Count == 50 && groups.Count < count);
        if (_session.CurrentUserId != user) return;
        Apply(groups, false);
        _skip = groups.Count; HasMore = batch.Count == 50;
        await PopulateRowsAsync();
        await SaveAsync();
    });

    [RelayCommand] private Task Refresh() => RefreshAsync();
    [RelayCommand] private Task More() => RunAsync(async () =>
    {
        var batch = await _api.MineAsync(_skip); _skip += batch.Count;
        Apply(batch, true); HasMore = batch.Count == 50;
        await PopulateRowsAsync();
        await SaveAsync();
    });
    private Task SaveAsync() => _cache.WriteAsync("my-groups", Groups.Select(g => g.Group).ToList());

    private async Task PopulateRowsAsync()
    {
        foreach (var item in Groups.ToArray())
        {
            await _rows.UpdateGroupAsync(item.Group, item.Row);
            await _messages.SyncChatAsync(item.Group.Id, group: true);
            item.Row.UnreadCount = await _unread.InitializeAsync(item.Group.Id, isGroup: true);
            var last = (await _messages.GetMessagesLocalAsync(item.Group.Id, count: 1, group: true)).FirstOrDefault();
            if (last != null) UpdateActivity(last);
        }
        Sort();
    }

    private void Apply(List<GroupProfile> profiles, bool append)
    {
        var existing = Groups.ToDictionary(g => g.Group.Id);
        var retained = profiles.Select(g => g.Id).ToHashSet();
        if (!append)
            foreach (var item in Groups.Where(g => !retained.Contains(g.Group.Id)).ToArray()) Groups.Remove(item);
        foreach (var profile in profiles.DistinctBy(g => g.Id))
        {
            if (!existing.TryGetValue(profile.Id, out var item))
            {
                item = new GroupListItem(profile, _rows.Create(profile.Name));
                item.Row.Subtitle = profile.Summary;
                Groups.Add(item);
            }
            item.Group = profile;
            item.Row.Title = profile.Name;
            if (item.LastActivity == default) item.Row.Subtitle = profile.Summary;
        }
        Sort();
    }

    private void UpdateActivity(MessageResponse message)
    {
        if (message.RecipientType != RecipientType.Group) return;
        var item = Groups.FirstOrDefault(g => g.Group.Id == message.RecipientId);
        if (item == null || message.SentAt < item.LastActivity) return;
        item.LastActivity = message.SentAt;
        item.Row.Subtitle = message.EncryptedContent;
        item.Row.SetLastMessageSentAt(message.SentAt);
        var local = message.SentAt.ToLocalTime();
        item.Row.DateTime = local.Date == DateTime.Today ? local.ToString("HH:mm")
            : local.Date == DateTime.Today.AddDays(-1) ? "Вчера" : local.ToString("dd.MM");
        Sort();
    }

    private void Sort()
    {
        var ordered = Groups.OrderByDescending(g => g.LastActivity == default ? g.Group.CreatedAt : g.LastActivity).ToArray();
        for (var i = 0; i < ordered.Length; i++) Groups.Move(Groups.IndexOf(ordered[i]), i);
    }

    private void OnUnreadCountChanged(Guid chatId, bool isGroup, int count)
    {
        if (!isGroup) return;
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var item = Groups.FirstOrDefault(g => g.Group.Id == chatId);
            if (item != null) item.Row.UnreadCount = count;
        });
    }

    [RelayCommand] private Task Explore() => Shell.Current.GoToAsync("GroupsExplorePage", false);
    [RelayCommand] private Task Open(GroupListItem item) => RunAsync(async () =>
    {
        GroupProfile profile;
        try { profile = await _api.GetAsync(item.Group.Id); }
        catch (GroupApiException ex) when (ex.Status is System.Net.HttpStatusCode.Forbidden or System.Net.HttpStatusCode.NotFound)
        {
            Groups.Remove(item); await SaveAsync();
            MainThread.BeginInvokeOnMainThread(async () => await RefreshAsync());
            throw;
        }
        if (profile.MyRole != null)
            await Shell.Current.GoToAsync($"chat?chatId={profile.Id}&isGroup=true", false);
        else
        {
            Groups.Remove(item); await SaveAsync();
            await OpenProfileAsync(profile.Id);
        }
    });
}
