using System;
using System.Collections.Generic;
using System.Text;

namespace Govor.Mobile.Models.Responses;

public class UploadMediaResponse
{
    public Guid MediaId { get; set; }
    public string Url { get; set; } = string.Empty;
}
