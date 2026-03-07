using Govor.Mobile.Services.Interfaces;

namespace Govor.Mobile.Services.Implementations;

public class WasOnlineFormater : IWasOnlineFormater
{
    public string FormatLastSeen(DateTime utcLastSeen)
    {
        var localTime = utcLastSeen.Kind == DateTimeKind.Utc
            ? utcLastSeen.ToLocalTime()
            : DateTime.SpecifyKind(utcLastSeen, DateTimeKind.Utc).ToLocalTime();

        var today = DateTime.Now.Date;

        if (localTime.Date == today)
            return localTime.ToString("HH:mm");

        if (localTime.Date == today.AddDays(-1))
            return "Вчера";

        if (localTime.Date == today.AddDays(-2))
            return "Позавчера";

        return localTime.ToString("dd.MM.yy");
    }

    public string FormatIsOnline(bool isOnline)
    {
        return isOnline ? "В сети" : "Был(а) недавно";
    }
}