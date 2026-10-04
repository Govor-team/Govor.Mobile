using System.Collections.ObjectModel;
using System.Net.Http.Headers;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Govor.Mobile.Models.Groups;
using Govor.Mobile.Models.Reactions;
using Govor.Mobile.PageModels.ContentViewsModel.Reactions;
using Govor.Mobile.Services.Api;
using Govor.Mobile.Services.Implementations;
using Govor.Mobile.Services.Interfaces;
using Govor.Mobile.Services.Interfaces.JwtServices;

namespace Govor.Mobile.PageModels.MainFlow.Groups;

public partial class PolicyReactionChoice(ReactionChoiceViewModel reaction) : ObservableObject
{
    public ReactionChoiceViewModel Reaction { get; } = reaction;
    [ObservableProperty] private bool selected;
    partial void OnSelectedChanged(bool value) => Reaction.IsSelected = value;
    [RelayCommand] private void Toggle() => Selected = !Selected;
}

public partial class GroupProfileModel(GroupsApiService api, GroupMediaService media,
    IJwtProviderService session, IServerIpProvider server, ReactionService reactions, GroupRowFactory rows, GroupRealtimeService realtime, GroupMemberContactService contacts) : GroupScreenModel
{
    public Guid GroupId { get; set; }
    public Func<GroupMemberItem, IReadOnlyList<string>, Task<string?>>? SelectMemberAction { get; set; }
    public Func<string, string, Task<bool>>? ConfirmMemberAction { get; set; }
    public event Action? AccessLost;
    public event Action? InvalidateMemberMenu;
    private IDisposable? _updates;
    private bool _visible;
    private bool _wasMember;

    public void Activate()
    {
        _visible = true;
        _updates?.Dispose();
        _updates = realtime.Subscribe(GroupId, _ =>
        {
            InvalidateMemberMenu?.Invoke();
            return RunAsync(async () => { if (_visible) await LoadCoreAsync(); });
        });
    }
    public void Deactivate()
    { _visible = false; _updates?.Dispose(); _updates = null; }

    [ObservableProperty] private GroupProfile? group;
    [ObservableProperty] private ImageSource? avatar;
    [ObservableProperty] private string name = "";
    [ObservableProperty] private string description = "";
    [ObservableProperty] private bool isPrivate;
    [ObservableProperty] private bool bannedOnly;
    [ObservableProperty] private bool hasMoreMembers;
    [ObservableProperty] private int section;
    [ObservableProperty] private string invitationDays = "7";
    [ObservableProperty] private string invitationLimit = "0";
    [ObservableProperty] private string invitationDescription = "";
    [ObservableProperty] private int reactionMode;
    [ObservableProperty] private string reactionSearchText = "";
    public ObservableCollection<GroupMemberItem> Members { get; } = new();
    public ObservableCollection<GroupInvitation> Invitations { get; } = new();
    public ObservableCollection<PolicyReactionChoice> PolicyChoices { get; } = new();
    public bool IsOwner => GroupPermissions.IsOwner(Group);
    public bool CanModerate => GroupPermissions.CanModerate(Group);
    public bool IsMember => Group?.MyRole != null;
    public bool CanJoin => Group != null && !IsMember && !Group.IsPrivate;
    public bool CanLeave => Group?.CanLeave == true;
    public bool HasAvatar => Avatar != null;
    public bool ShowMembers => IsMember && Section == 0;
    public bool ShowInvitations => CanModerate && Section == 1;
    public bool ShowPolicy => CanModerate && Group?.IsChannel == true && Section == 2;
    public bool CanSetPolicy => CanModerate && Group?.IsChannel == true;
    public bool SelectReactions => ReactionMode == 2;
    private int _memberSkip;
    private bool _policyLoaded;
    private readonly List<PolicyReactionChoice> _allPolicyChoices = new();
    public string PolicySelectionSummary => $"Выбрано: {_allPolicyChoices.Count(c => c.Selected)} · Доступно: {_allPolicyChoices.Count}";
    private HashSet<Guid> _unlistedPolicyIds = new();

    partial void OnGroupChanged(GroupProfile? value)
    {
        foreach (var property in new[] { nameof(IsOwner), nameof(CanModerate), nameof(IsMember), nameof(CanJoin),
            nameof(CanLeave), nameof(ShowMembers), nameof(ShowInvitations), nameof(ShowPolicy), nameof(CanSetPolicy) })
            OnPropertyChanged(property);
    }
    partial void OnAvatarChanged(ImageSource? value) => OnPropertyChanged(nameof(HasAvatar));
    partial void OnSectionChanged(int value)
    { OnPropertyChanged(nameof(ShowMembers)); OnPropertyChanged(nameof(ShowInvitations)); OnPropertyChanged(nameof(ShowPolicy)); }
    partial void OnReactionModeChanged(int value) => OnPropertyChanged(nameof(SelectReactions));
    partial void OnReactionSearchTextChanged(string value) => FilterPolicyChoices();

    private void FilterPolicyChoices()
    {
        PolicyChoices.Clear();
        foreach (var choice in _allPolicyChoices.Where(c => string.IsNullOrWhiteSpace(ReactionSearchText)
            || (c.Reaction.Definition.Name + c.Reaction.Definition.Emoji + c.Reaction.Definition.Code)
                .Contains(ReactionSearchText, StringComparison.OrdinalIgnoreCase)))
            PolicyChoices.Add(choice);
    }

    private void PolicyChoiceChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PolicyReactionChoice.Selected))
            OnPropertyChanged(nameof(PolicySelectionSummary));
    }

    public Task LoadAsync() => RunAsync(LoadCoreAsync);
    private async Task LoadCoreAsync()
    {
        // Never reuse a cached role to authorize operations or show cached private history.
        Group = await api.GetAsync(GroupId);
        if (_wasMember && Group.MyRole == null) { LoseAccess(); return; }
        _wasMember |= IsMember;
        Name = Group.Name; Description = Group.Description; IsPrivate = Group.IsPrivate;
        Avatar = await media.LoadAsync(Group);
        if (IsMember)
        {
            if (!CanModerate) { BannedOnly = false; Section = 0; }
            await LoadMembersCoreAsync();
            if (ShowInvitations) await LoadInvitationsCoreAsync();
            if (ShowPolicy) await LoadPolicyCoreAsync();
        }
        else { Members.Clear(); Invitations.Clear(); PolicyChoices.Clear(); }
    }
    protected override async Task AccessDeniedAsync()
    {
        try { Group = await api.GetAsync(GroupId); }
        catch { Group = null; }
        if (Group == null || (_wasMember && !IsMember)) { LoseAccess(); return; }
        Members.Clear(); Invitations.Clear(); PolicyChoices.Clear();
        if (!CanModerate) { BannedOnly = false; Section = 0; }
        if (Group == null)
        {
            Name = "Нет доступа к сообществу"; Description = ""; Avatar = null;
            return;
        }
        if (IsMember)
        {
            try
            {
                await LoadMembersCoreAsync();
                if (ShowInvitations) await LoadInvitationsCoreAsync();
            }
            catch { /* Preserve the original command error; refresh remains available. */ }
        }
    }
    private void LoseAccess()
    {
        Group = null; Avatar = null;
        Members.Clear(); Invitations.Clear(); PolicyChoices.Clear();
        InvalidateMemberMenu?.Invoke();
        if (_visible) AccessLost?.Invoke();
    }
    [RelayCommand] private Task Refresh() => LoadAsync();
    [RelayCommand] private Task Save() => RunAsync(async () =>
    {
        if (!IsOwner) return;
        if (Name.Trim().Length is < 1 or > 100 || Description.Length > 500)
            throw new ArgumentException("Имя: 1–100 символов; описание: до 500.");
        Group = await api.SaveAsync(GroupId, Name, Description, IsPrivate);
        await LoadCoreAsync(); Status = "Профиль сохранён.";
    });
    [RelayCommand] private Task Join() => RunAsync(async () =>
    { if (!CanJoin) return; Group = await api.JoinAsync(GroupId); await LoadCoreAsync(); });
    [RelayCommand] private Task OpenChat() => RunAsync(async () =>
    {
        Group = await api.GetAsync(GroupId);
        if (!IsMember) throw new InvalidOperationException("Сначала вступите в сообщество.");
        await Shell.Current.GoToAsync($"chat?chatId={GroupId}&isGroup=true", false);
    });
    [RelayCommand] private Task Leave() => RunAsync(async () =>
    {
        if (!CanLeave || !await ConfirmAsync("Выйти из сообщества?", "Вы потеряете доступ к истории и участникам.")) return;
        await api.LeaveAsync(GroupId);
        await Shell.Current.GoToAsync("..", false);
    });
    [RelayCommand] private Task UploadAvatar() => RunAsync(async () =>
    {
        if (!IsOwner) return;
        var picked = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = "Аватар PNG, JPEG или WebP", FileTypes = FilePickerFileType.Images });
        if (picked == null) return;
        var ext = Path.GetExtension(picked.FileName).ToLowerInvariant();
        var mime = ext switch { ".png" => "image/png", ".jpg" or ".jpeg" => "image/jpeg", ".webp" => "image/webp", _ => throw new ArgumentException("Поддерживаются статичные PNG, JPEG и WebP.") };
        await using var stream = await picked.OpenReadAsync();
        using var bytes = new MemoryStream();
        var buffer = new byte[8192];
        int read;
        while ((read = await stream.ReadAsync(buffer)) > 0)
        {
            if (bytes.Length + read > 2 * 1024 * 1024) throw new ArgumentException("Аватар должен быть не больше 2 МиБ.");
            bytes.Write(buffer, 0, read);
        }
        using var form = new MultipartFormDataContent();
        var content = new ByteArrayContent(bytes.ToArray()); content.Headers.ContentType = new MediaTypeHeaderValue(mime);
        form.Add(content, "file", picked.FileName);
        await api.UploadAvatarAsync(GroupId, form);
        await LoadCoreAsync();
    });
    [RelayCommand] private Task DeleteAvatar() => RunAsync(async () =>
    {
        if (!IsOwner || !HasAvatar || !await ConfirmAsync("Убрать аватар?", "Текущий аватар сообщества будет снят.")) return;
        await api.DeleteAvatarAsync(GroupId); await LoadCoreAsync();
    });
    private async Task LoadMembersCoreAsync()
    {
        var count = Math.Max(50, _memberSkip);
        var members = new List<GroupMember>();
        List<GroupMember> batch;
        do
        {
            batch = await api.MembersAsync(GroupId, BannedOnly, members.Count);
            members.AddRange(batch);
        } while (batch.Count == 50 && members.Count < count);
        var items = await CreateMemberRowsAsync(members);
        Members.Clear(); foreach (var item in items) Members.Add(item);
        _memberSkip = members.Count; HasMoreMembers = batch.Count == 50;
    }
    private async Task<List<GroupMemberItem>> CreateMemberRowsAsync(IEnumerable<GroupMember> members)
    {
        // Limit profile/avatar requests when loading a large participant list.
        using var limit = new SemaphoreSlim(4, 4);
        var tasks = members.Select(async member =>
        {
            await limit.WaitAsync();
            try { return await rows.CreateMemberAsync(member); }
            finally { limit.Release(); }
        });
        return (await Task.WhenAll(tasks)).ToList();
    }
    [RelayCommand] private Task ShowActiveMembers() => RunAsync(async () => { Section = 0; BannedOnly = false; _memberSkip = 0; await LoadMembersCoreAsync(); });
    [RelayCommand] private Task ShowBannedMembers() => RunAsync(async () => { if (!CanModerate) return; Section = 0; BannedOnly = true; _memberSkip = 0; await LoadMembersCoreAsync(); });
    [RelayCommand] private Task MoreMembers() => RunAsync(async () =>
    {
        var batch = await api.MembersAsync(GroupId, BannedOnly, _memberSkip); _memberSkip += batch.Count;
        foreach (var item in await CreateMemberRowsAsync(batch.Where(m => Members.All(x => x.UserId != m.UserId)))) Members.Add(item);
        HasMoreMembers = batch.Count == 50;
    });
    [RelayCommand] private Task ManageMember(GroupMemberItem item) => RunAsync(async () =>
    {
        // Revalidate both the actor and the selected member before presenting available actions.
        Group = await api.GetAsync(GroupId);
        await LoadMembersCoreAsync();
        var current = Members.FirstOrDefault(m => m.UserId == item.UserId);
        if (current == null) return;
        var member = current.Member;
        var actor = session.CurrentUserId ?? Guid.Empty;
        var options = new List<string>();
        var contact = await contacts.GetStateAsync(member.UserId);
        if (contact.Kind != MemberContactKind.Self) options.Add(contact.ActionTitle);
        if (GroupPermissions.CanChangeRole(Group, member, actor))
        {
            options.Add(member.Role == GroupRole.Admin ? "Снять администратора" : "Назначить администратором");
            options.Add("Передать владение");
        }
        if (GroupPermissions.CanBan(Group, member, actor)) options.Add(member.IsBanned ? "Разблокировать" : "Заблокировать");
        if (options.Count == 0) { Status = $"{member.Username} · {member.RoleLabel}"; return; }
        var action = SelectMemberAction == null ? null : await SelectMemberAction(current, options);
        if (action == contact.ActionTitle && contact.CanAct)
        {
            Status = await contacts.ActAsync(member.UserId, contact.Kind);
            return;
        }
        switch (action)
        {
            case "Назначить администратором":
                if (await ConfirmModerationAsync(action, $"Дать {member.Username} права модерации?")) await api.SetAdminAsync(GroupId, member.UserId, true);
                break;
            case "Снять администратора":
                if (await ConfirmModerationAsync(action, $"Снять права у {member.Username}?")) await api.SetAdminAsync(GroupId, member.UserId, false);
                break;
            case "Заблокировать":
                if (await ConfirmModerationAsync(action, $"{member.Username} потеряет доступ к сообществу.")) await api.SetBanAsync(GroupId, member.UserId, true);
                break;
            case "Разблокировать": await api.SetBanAsync(GroupId, member.UserId, false); break;
            case "Передать владение":
                if (await ConfirmModerationAsync(action, $"{member.Username} станет владельцем, вы — администратором.")) await api.TransferAsync(GroupId, member.UserId);
                break;
            default: return;
        }
        await LoadCoreAsync();
    });
    private Task<bool> ConfirmModerationAsync(string title, string detail) =>
        ConfirmMemberAction?.Invoke(title, detail) ?? Task.FromResult(false);

    private async Task LoadInvitationsCoreAsync()
    {
        var items = await api.InvitationsAsync(GroupId);
        Invitations.Clear(); foreach (var invitation in items) Invitations.Add(invitation);
    }
    [RelayCommand] private Task ShowInvites() => RunAsync(async () => { if (!CanModerate) return; Section = 1; await LoadInvitationsCoreAsync(); });
    [RelayCommand] private Task CreateInvite() => RunAsync(async () =>
    {
        if (!CanModerate) return;
        if (!int.TryParse(InvitationDays, out var days) || days is < 1 or > 365 ||
            !int.TryParse(InvitationLimit, out var limit) || limit is < 0 or > 1000000)
            throw new ArgumentException("Срок: 1–365 дней. Лимит: 0–1 000 000; 0 — без ограничения.");
        await api.CreateInvitationAsync(GroupId, days, limit, InvitationDescription);
        await LoadInvitationsCoreAsync(); Status = "Приглашение создано.";
    });
    [RelayCommand] private Task CopyInvite(GroupInvitation invitation) => RunAsync(async () =>
    {
        if (!invitation.IsActive) throw new InvalidOperationException("Приглашение уже недействительно.");
        // Construct the documented route against our own base URL, not an arbitrary returned host.
        var link = new Uri(new Uri(server.IP), $"/invite/{Uri.EscapeDataString(invitation.InvitationCode)}");
        await Clipboard.Default.SetTextAsync(link.ToString()); Status = "Ссылка скопирована.";
    });
    [RelayCommand] private Task RevokeInvite(GroupInvitation invitation) => RunAsync(async () =>
    {
        if (!CanModerate || !invitation.IsActive || !await ConfirmAsync("Отозвать приглашение?", "По этой ссылке больше нельзя будет вступить.")) return;
        await api.RevokeInvitationAsync(GroupId, invitation.Id); await LoadInvitationsCoreAsync();
    });
    private async Task LoadPolicyCoreAsync()
    {
        var policy = await api.PolicyAsync(GroupId);
        ReactionMode = policy.Mode;
        var packs = await reactions.GetPacksAsync(catalog: true);
        foreach (var previous in _allPolicyChoices) previous.PropertyChanged -= PolicyChoiceChanged;
        _allPolicyChoices.Clear();
        foreach (var definition in packs.Where(p => p.IsEnabled).SelectMany(p => p.Reactions).Where(r => r.IsEnabled).DistinctBy(r => r.Id))
        {
            var choice = new ReactionChoiceViewModel(definition);
            var option = new PolicyReactionChoice(choice) { Selected = policy.ReactionIds.Contains(definition.Id) };
            option.PropertyChanged += PolicyChoiceChanged;
            _allPolicyChoices.Add(option);
            _ = choice.LoadMediaAsync(reactions);
        }
        FilterPolicyChoices();
        OnPropertyChanged(nameof(PolicySelectionSummary));
        _unlistedPolicyIds = policy.ReactionIds.Except(_allPolicyChoices.Select(c => c.Reaction.Definition.Id)).ToHashSet();
        _policyLoaded = true;
    }
    [RelayCommand] private Task ShowReactions() => RunAsync(async () => { if (!CanSetPolicy) return; Section = 2; _policyLoaded = false; await LoadPolicyCoreAsync(); });
    [RelayCommand] private Task SavePolicy() => RunAsync(async () =>
    {
        if (!CanSetPolicy || !_policyLoaded) return;
        await api.SetPolicyAsync(GroupId, ReactionMode, _allPolicyChoices.Where(r => r.Selected).Select(r => r.Reaction.Definition.Id).Concat(_unlistedPolicyIds));
        Status = "Настройки реакций сохранены.";
    });
}
