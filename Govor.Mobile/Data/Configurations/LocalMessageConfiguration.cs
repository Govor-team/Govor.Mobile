using System.Text.Json;
using Govor.Mobile.Models;
using Govor.Mobile.Models.Responses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Govor.Mobile.Data.Configurations;

public class LocalMessageConfiguration : IEntityTypeConfiguration<LocalMessage>
{
    public void Configure(EntityTypeBuilder<LocalMessage> builder)
    {
        // Настраиваем первичный ключ (на всякий случай)
        builder.HasKey(e => e.Id);

        // Индекс для ускорения поиска по чату
        builder.HasIndex(e => e.ChatId);

        // КОНВЕРТАЦИЯ СПИСКА В JSON СТРОКУ
        builder.Property(e => e.MediaAttachments)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                v => JsonSerializer.Deserialize<List<MediaFile>>(v, (JsonSerializerOptions)null) ?? new List<MediaFile>() 
            );

        builder.Property(e => e.Reactions)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                v => JsonSerializer.Deserialize<List<MessageReactionResponse>>(v, (JsonSerializerOptions)null) ?? new List<MessageReactionResponse>()
            );
    }
}