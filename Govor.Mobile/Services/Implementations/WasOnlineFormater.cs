using Govor.Mobile.Services.Interfaces;

namespace Govor.Mobile.Services.Implementations;

public class WasOnlineFormater : IWasOnlineFormater
{
    public string FormatLastSeen(DateTime lastSeen)
    {
        if (lastSeen.Date == DateTime.Today)
            return lastSeen.ToString("HH:mm"); // "14:20"
    
        if (lastSeen.Date == DateTime.Today.AddDays(-1))
            return "Вчера"; 
        
        if (lastSeen.Date == DateTime.Today.AddDays(-2))
            return "Позовчера"; 
        
        return lastSeen.ToString("dd.MM.yy"); // "25.12.25"
    }
}