using Govor.Mobile.Models.Responses;
using System;
using System.Collections.Generic;
using System.Text;

namespace Govor.Mobile.Data;

public class MessageView
{
    public Guid Id { get; set; }
    public Guid MessageId { get; set; }
    public Guid ChatId { get; set; }
    public RecipientType RecipientType { get; set; }
    public Guid UserId { get; set; }
    public DateTime ViewedAt { get; set; }
}
