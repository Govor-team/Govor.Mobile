using Govor.Mobile.Models.Requests;

namespace Govor.Mobile.Models;

public class MediaFile
{
    public Guid Id { get; set; }
    public Guid UploaderId { get; set; }
    public string Url { get; set; }
    public MediaType MediaType { get; set; }
    public string MineType { get; set; }
    public DateTime DateCreated { get; set; }

    public MediaOwnerType OwnerType { get; set; } = MediaOwnerType.Message;
    public Guid? OwnerId { get; set; }
}

public enum MediaOwnerType
{
    Message = 0,   
    Avatar = 1,    
    GroupAvatar = 2, 
    System = 3     // (Emoge, icons � e.t.c)
}