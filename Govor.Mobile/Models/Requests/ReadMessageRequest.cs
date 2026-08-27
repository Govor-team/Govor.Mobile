using System;
using System.Collections.Generic;
using System.Text;

namespace Govor.Mobile.Models.Requests;

public class ReadMessageRequest
{
    public Guid MessageId { get; set; }
}
