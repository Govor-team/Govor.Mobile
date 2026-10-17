namespace Govor.Mobile.Models;

public sealed record ChatPushNotification(Guid ChatId, Guid MessageId, Guid SenderId, bool IsGroup)
{
    public static bool TryParse(IDictionary<string, object> data, out ChatPushNotification? notification)
    {
        notification = null;
        string? Value(string key) => data.TryGetValue(key, out var value) ? value?.ToString() : null;
        if (Value("type") != "chat_message" ||
            !Guid.TryParse(Value("chatId"), out var chat) || chat == Guid.Empty ||
            !Guid.TryParse(Value("messageId"), out var message) || message == Guid.Empty ||
            !Guid.TryParse(Value("senderId"), out var sender) || sender == Guid.Empty ||
            !bool.TryParse(Value("isGroup"), out var group)) return false;
        notification = new(chat, message, sender, group);
        return true;
    }
}
