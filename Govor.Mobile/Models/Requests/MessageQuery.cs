namespace Govor.Mobile.Models.Requests;

public class MessageQuery
{
    public Guid? StartMessageId { get; set; }
    public int Before { get; set; } = 20;
    public int After { get; set; } = 2;
}
