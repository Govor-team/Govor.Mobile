using System.Globalization;

namespace Govor.Mobile.Utilities;

public static class ChatDateFormatter
{
    private static readonly CultureInfo Russian = CultureInfo.GetCultureInfo("ru-RU");

    public static string Format(DateTime localDate, DateTime today)
    {
        var date = localDate.Date;
        if (date == today.Date) return "Сегодня";
        if (date == today.Date.AddDays(-1)) return "Вчера";
        return date.ToString(date.Year == today.Year ? "d MMMM" : "d MMMM yyyy", Russian);
    }
}
