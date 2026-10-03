using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using Govor.Mobile.Utilities;

namespace Govor.Mobile.PageModels.ContentViewsModel.Messages;

public partial class MessagesGroupModel : ObservableObject
{
    public DateTime LocalDate { get; init; }
    public string DateLabel => ChatDateFormatter.Format(LocalDate, DateTime.Today);
    [ObservableProperty] private bool showsDateSeparator;
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
