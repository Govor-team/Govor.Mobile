using Govor.Mobile.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Govor.Mobile.Data.Configurations;

public class LocalUserFriendsConfiguration : IEntityTypeConfiguration<LocalUserProfile>
{
    public void Configure(EntityTypeBuilder<LocalUserProfile> builder)
    {
        // Настраиваем первичный ключ (на всякий случай)
        builder.HasKey(e => e.UserId);

        
    }
}