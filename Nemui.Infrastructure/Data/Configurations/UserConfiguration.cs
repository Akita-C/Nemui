using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nemui.Shared.Constants;
using Nemui.Shared.Entities;

namespace Nemui.Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable(DatabaseConstants.TableNames.Users);
        
        builder.HasKey(x => x.Id);
        
        builder.Property(u => u.Email)
            .HasMaxLength(DatabaseConstants.FieldLengths.EmailMaxLength)
            .IsRequired();

        builder.Property(u => u.Name)
            .HasMaxLength(DatabaseConstants.FieldLengths.NameMaxLength)
            .IsRequired();

        builder.Property(u => u.PasswordHash)
            .IsRequired();

        builder.Property(u => u.Role)
            .HasMaxLength(DatabaseConstants.FieldLengths.RoleMaxLength)
            .HasDefaultValue(AuthConstants.Roles.User);

        builder.Property(u => u.IsEmailVerified)
            .HasDefaultValue(false);

        builder.Property(u => u.IsActive)
            .HasDefaultValue(true);

        builder.Property(u => u.IsDeleted)
            .HasDefaultValue(false);

        builder.Property(u => u.FailedLoginAttempts)
            .HasDefaultValue(0);

        builder.HasIndex(u => u.Email)
            .IsUnique()
            .HasFilter(DatabaseConstants.Filters.SoftDeleteFilter) // User đang active mới bị index này constraint
            .HasDatabaseName(DatabaseConstants.Indexes.UserEmailUniqueIndex); 

        builder.HasIndex(u => u.IsDeleted)
            .HasDatabaseName(DatabaseConstants.Indexes.UserIsDeletedIndex);
        builder.HasIndex(u => u.CreatedAt)
            .HasDatabaseName(DatabaseConstants.Indexes.UserCreatedAtIndex);

        builder.HasMany(u => u.RefreshTokens)
            .WithOne(rt => rt.User)
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Global query filter for soft delete 
        builder.HasQueryFilter(u => !u.IsDeleted);
    }
}