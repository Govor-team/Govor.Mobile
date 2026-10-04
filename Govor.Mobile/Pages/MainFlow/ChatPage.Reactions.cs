using System.Collections.Specialized;
using Govor.Mobile.Models.Reactions;
using Govor.Mobile.PageModels.ContentViewsModel.Messages;
using Govor.Mobile.PageModels.ContentViewsModel.Reactions;
using Govor.Mobile.PageModels.MainFlow;
using Govor.Mobile.Pages.ContentViews;
using Govor.Mobile.Services.Implementations;

namespace Govor.Mobile.Pages.MainFlow;

public partial class ChatPage
{
    private readonly ReactionService _reactions;
    private readonly HashSet<Guid> _hydrated = new();
    private readonly Dictionary<Guid, DateTime> _lastReactionRefresh = new();
    private MessageReactionMenu? _messageMenu;
    private bool _changingReaction;

    private void ReactionGroupsChanged(object? sender, NotifyCollectionChangedEventArgs e) => HydrateReactions();

    private async void HydrateReactions()
    {
        if (!_pageVisible || BindingContext is not ChatPageModel model)
            return;

        foreach (var message in model.MessageGroups.SelectMany(g => g.Messages).ToArray())
        {
            if (!_hydrated.Add(message.Id))
                continue;

            try
            {
                var state = await _reactions.GetStateAsync(message.Id);
                ApplyReactionState(message, state);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }
    }

    private void ReactionsChanged(MessageReactionState state) => Dispatcher.Dispatch(() =>
    {
        if (!_pageVisible || BindingContext is not ChatPageModel model)
            return;

        var message = model.MessageGroups.SelectMany(g => g.Messages)
            .FirstOrDefault(m => m.Id == state.MessageId);
        if (message != null)
            ApplyReactionState(message, state);
    });

    private void ReactionsReconnected() => Dispatcher.Dispatch(() =>
    {
        _lastReactionRefresh.Clear();
        RefreshVisibleReactions();
    });

    private async void RefreshVisibleReactions()
    {
        if (!_pageVisible || _firstVisible < 0 || BindingContext is not ChatPageModel model)
            return;

        var messages = model.MessageGroups.Skip(_firstVisible)
            .Take(_lastVisible - _firstVisible + 1)
            .SelectMany(g => g.Messages).ToArray();

        foreach (var message in messages)
        {
            if (_lastReactionRefresh.TryGetValue(message.Id, out var last)
                && DateTime.UtcNow - last < TimeSpan.FromMinutes(1))
                continue;

            _lastReactionRefresh[message.Id] = DateTime.UtcNow;
            try
            {
                await _reactions.RefreshAsync(message.Id);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }
    }

    private void ApplyReactionState(MessagesViewModel message, MessageReactionState state)
    {
        message.ReactionChips.Clear();
        foreach (var count in state.Counts)
        {
            var chip = new ReactionChip
            {
                MessageId = message.Id,
                ReactionId = count.ReactionId,
                Text = count.ReactionCode,
                CountText = count.Count.ToString(),
                IsMine = count.ReactionId != null
                    ? state.OwnReactionId == count.ReactionId
                    : state.OwnReactionCode == count.ReactionCode
            };
            message.ReactionChips.Add(chip);
            if (count.ReactionId is Guid id)
                _ = LoadChipMediaAsync(chip, id);
        }
    }

    private async Task LoadChipMediaAsync(ReactionChip chip, Guid id)
    {
        try
        {
            var definition = await _reactions.GetDefinitionAsync(id);
            if (definition is null || definition.Kind == 0)
                return;

            var path = await _reactions.GetMediaPathAsync(definition);
            if (path != null)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    chip.Image = ImageSource.FromFile(path);
                    chip.Text = "";
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex.Message);
        }
    }

    private async void ToggleReaction(ReactionChip? chip)
    {
        if (chip == null || _changingReaction)
            return;

        _changingReaction = true;
        try
        {
            if (chip.ReactionId is Guid id)
                await _reactions.ToggleAsync(chip.MessageId, id);
            else if (chip.IsMine)
                await _reactions.RemoveAsync(chip.MessageId);
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Реакция", ex.Message, "OK");
        }
        finally
        {
            _changingReaction = false;
        }
    }

    private void CloseMessageMenu()
    {
        if (_messageMenu == null)
            return;

        var menu = _messageMenu;
        _messageMenu = null;
        menu.ViewModel.Close();
        if (Content is Grid root)
            root.Children.Remove(menu);
    }

    protected override bool OnBackButtonPressed()
    {
        if (_userProfileSheet != null) { CloseUserProfile(); return true; }
        if (_messageMenu == null)
            return base.OnBackButtonPressed();

        CloseMessageMenu();
        return true;
    }

    private async void ShowMessageMenu(MessagesViewModel message)
    {
        CloseMessageMenu();
        if (Content is not Grid root || BindingContext is not ChatPageModel chat)
            return;

        MessageEditor.Unfocus();
        var model = new MessageReactionMenuModel(_reactions, message, chat.IsGroup ? chat.ChatId : null,
            chat.CanWrite && message.IsOwnMessage, message.IsOwnMessage || (chat.IsGroup && chat.CanModerateGroup),
            isChannel: chat.GroupProfile?.IsChannel == true);
        model.CloseRequested += CloseMessageMenu;
        model.ActionRequested += action => HandleMessageAction(action, message);

        var menu = new MessageReactionMenu(model);
        Grid.SetRowSpan(menu, 3);
        root.Children.Add(menu);
        _messageMenu = menu;
        await model.InitializeAsync();
    }

    private async void HandleMessageAction(MessageMenuAction action, MessagesViewModel message)
    {
        CloseMessageMenu();
        if (BindingContext is not ChatPageModel chat)
            return;

        switch (action)
        {
            case MessageMenuAction.OpenPacks:
                await Shell.Current.GoToAsync(nameof(ReactionPacksPage));
                break;
            case MessageMenuAction.Copy:
                await Clipboard.Default.SetTextAsync(message.Text);
                break;
            case MessageMenuAction.Select:
                chat.ToggleMessageSelectionCommand.Execute(message);
                break;
            case MessageMenuAction.Edit:
                chat.ToggleMessageSelectionCommand.Execute(message);
                chat.EditMessageCommand.Execute(null);
                break;
            case MessageMenuAction.Delete:
                chat.ToggleMessageSelectionCommand.Execute(message);
                await chat.DeleteMessagesCommand.ExecuteAsync(null);
                break;
        }
    }
}
