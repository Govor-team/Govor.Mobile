using System;
using System.Collections.Generic;
using System.Text;

namespace Govor.Mobile.Models.Requests;

public class UploadMediaRequest
{
    public Stream FileStream { get; set; } = Stream.Null;
    public string FileName { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public MediaType Type { get; set; } = MediaType.Image;
    public MediaOwnerType OwnerType { get; set; } = MediaOwnerType.Message;
    public string EncryptedKey { get; set; } = string.Empty;
}

public enum MediaType
{
    Image,
    Video,
    Audio,
    File,
    Voice
}

public enum MediaOwnerType
{
    Message = 0,   
    Avatar = 1,    
    GroupAvatar = 2, 
    System = 3     // (Emoge, icons � e.t.c)
}