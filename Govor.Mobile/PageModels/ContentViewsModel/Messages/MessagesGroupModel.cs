using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace Govor.Mobile.PageModels.ContentViewsModel.Messages;

public class MessagesGroupModel
{
    public bool IsIncoming { get; init; }

    public Guid SenderId { get; init; }
    public AvatarViewModel Avatar { get; init; }
    public ObservableCollection<MessagesViewModel> Messages { get; } = new();
}

public enum MessageGroupPosition
{
    Single,
    First,
    Middle,
    Last
}