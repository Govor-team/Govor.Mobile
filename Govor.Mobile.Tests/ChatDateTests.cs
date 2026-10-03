using Govor.Mobile.Utilities;

namespace Govor.Mobile.Tests;

public class ChatDateTests
{
    [TestCase("2026-10-03T00:01:00", "2026-10-03", "Сегодня")]
    [TestCase("2026-10-02T23:59:00", "2026-10-03", "Вчера")]
    [TestCase("2026-09-30", "2026-10-03", "30 сентября")]
    [TestCase("2025-10-03", "2026-10-03", "3 октября 2025")]
    [TestCase("2025-12-31T23:59:00", "2026-01-01", "Вчера")]
    public void DateLabelsUseCalendarDaysAndDisambiguatePreviousYears(string date, string today, string expected)
    {
        Assert.That(ChatDateFormatter.Format(DateTime.Parse(date), DateTime.Parse(today)), Is.EqualTo(expected));
    }
}
