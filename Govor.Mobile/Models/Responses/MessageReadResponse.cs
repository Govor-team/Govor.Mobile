using System;
using System.Collections.Generic;
using System.Text;

namespace Govor.Mobile.Models.Responses;

public class MessageReadResponse
{
    public required Guid ViewId { get; set; }
    public required Guid MessageId { get; set; }
    public required Guid ReaderId { get; set; }
    public required Guid RecipientId { get; set; }
    public required DateTime WhenWas { get; set; }
    public RecipientType RecipientType { get; set; }
}