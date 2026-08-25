namespace Govor.Mobile.Models.Requests;

public class RemoveMessageRequest
{
    public Guid MessageId { get; set; }
    public RemoveMessageRequestType RequestType { get; set; }
}

public enum RemoveMessageRequestType : int
{
    HideForMe = 0,
    ForceRemove = 1
}