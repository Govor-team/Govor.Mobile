using System.Text.Json;

namespace Govor.Mobile.Services.Implementations.Notification;

public static class PushMessageHistory
{
    public const int Limit = 256;

    public static bool TryRemember(string history, Guid messageId, out string updated)
    {
        Guid[] ids;
        try { ids = JsonSerializer.Deserialize<Guid[]>(history) ?? []; }
        catch (JsonException) { ids = []; }
        updated = history;
        if (ids.Contains(messageId)) return false;
        updated = JsonSerializer.Serialize(ids.Append(messageId).Distinct().TakeLast(Limit));
        return true;
    }
}
