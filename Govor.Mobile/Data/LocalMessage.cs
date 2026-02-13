using System.ComponentModel.DataAnnotations;
using Govor.Mobile.Models;
using Govor.Mobile.Models.Responses;

namespace Govor.Mobile.Data;

public class LocalMessage
{
    [Key]
    public Guid Id { get; set; }

    public Guid SenderId { get; set; }
    
    // ID чата (User ID собеседника или Group ID) для фильтрации
    public Guid ChatId { get; set; } 

    public RecipientType RecipientType { get; set; }
    public string EncryptedContent { get; set; }
    public DateTime SentAt { get; set; }
    
    public bool IsEdited { get; set; }
    public DateTime? EditedAt { get; set; }
    public Guid? ReplyToMessageId { get; set; }

    // Локальный статус (0 - Synced, 1 - Sending, 2 - Error)
    public int LocalStatus { get; set; } = 0;

    // --- Сложные типы (Списки) ---
    // EF Core по умолчанию их проигнорирует или попытается создать связь таблиц.
    // Мы настроим их хранение как JSON внутри этого же класса.
    public List<MediaFile> MediaAttachments { get; set; } = new();
    public List<MessageReactionResponse> Reactions { get; set; } = new();
    // MessageViews обычно не нужны оффлайн в полном объеме, но можно добавить так же
}