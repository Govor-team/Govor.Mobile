using Govor.Mobile.Services.Interfaces.Notification;

namespace Govor.Mobile.Services.Implementations.Notification;

public class MessageWasSendedFormater : IMessageWasSendedFormater
{
    public string Format(DateTime time)
    {
        var localTime = time.Kind == DateTimeKind.Utc
            ? time.ToLocalTime()
            : DateTime.SpecifyKind(time, DateTimeKind.Utc).ToLocalTime();

        var today = DateTime.Now.Date;

        if (localTime.Date == today)
            return localTime.ToString("HH:mm");

        if (localTime.Date == today.AddDays(-1))
            return "Вчера";

        if (localTime.Date == today.AddDays(-2))
            return "Позавчера";

        return localTime.ToString("dd.MM.yy");
    }
}