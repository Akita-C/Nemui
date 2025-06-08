using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nemui.Shared.Constants;
using Nemui.Shared.Entities;

namespace Nemui.Infrastructure.Data.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable(DatabaseConstants.TableNames.RefreshTokens);

        builder.HasKey(rt => rt.Id);

        builder.Property(rt => rt.Token)
            .IsRequired()
            .HasMaxLength(DatabaseConstants.FieldLengths.TokenMaxLength);

        builder.Property(rt => rt.ExpiresAt)
            .IsRequired();

        // Indexes
        builder.HasIndex(rt => rt.Token)
            .IsUnique()
            .HasDatabaseName(DatabaseConstants.Indexes.RefreshTokenTokenUniqueIndex);

        builder.HasIndex(rt => rt.UserId)
            .HasDatabaseName(DatabaseConstants.Indexes.RefreshTokenUserIdIndex);
        
        builder.HasIndex(rt => rt.ExpiresAt)
            .HasDatabaseName(DatabaseConstants.Indexes.RefreshTokenExpiresAtIndex);
        
        builder.HasIndex(rt => rt.RevokedAt);
        builder.HasIndex(rt => rt.CreatedAt);

        // Relationships
        builder.HasOne(rt => rt.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(rt => !rt.User.IsDeleted);
    }
}