using System.Text.Json;
using Govor.Mobile.Models;
using Govor.Mobile.Models.Responses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.ChangeTracking;

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

        builder.Property(e => e.MessageViews)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                v => string.IsNullOrWhiteSpace(v) ? new List<MessageView>() : JsonSerializer.Deserialize<List<MessageView>>(v, (JsonSerializerOptions)null) ?? new List<MessageView>()
            );
        builder.Property(e => e.MediaAttachments).Metadata.SetValueComparer(ListComparer<MediaFile>());
        builder.Property(e => e.Reactions).Metadata.SetValueComparer(ListComparer<MessageReactionResponse>());
        builder.Property(e => e.MessageViews).Metadata.SetValueComparer(ListComparer<MessageView>());
    }

    private static ValueComparer<List<T>> ListComparer<T>() => new(
        (left, right) => JsonSerializer.Serialize(left, (JsonSerializerOptions?)null) == JsonSerializer.Serialize(right, (JsonSerializerOptions?)null),
        value => JsonSerializer.Serialize(value, (JsonSerializerOptions?)null).GetHashCode(),
        value => JsonSerializer.Deserialize<List<T>>(JsonSerializer.Serialize(value, (JsonSerializerOptions?)null), (JsonSerializerOptions?)null)!);
}
