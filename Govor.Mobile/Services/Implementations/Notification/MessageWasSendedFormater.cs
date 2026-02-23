using Govor.Mobile.Services.Interfaces.Notification;

namespace Govor.Mobile.Services.Implementations.Notification;

public class MessageWasSendedFormater : IMessageWasSendedFormater
{
    public string Format(DateTime time)
    {
        if (time.Date == DateTime.Today)
            return time.ToString("HH:mm"); // "14:20"
    
        if (time.Date == DateTime.Today.AddDays(-1))
            return "Вчера"; 
        
        if (time.Date == DateTime.Today.AddDays(-2))
            return "Позовчера"; 
        
        return time.ToString("dd.MM.yy"); // "25.12.25"
    }
}