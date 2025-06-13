using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nemui.Shared.Constants;
using Nemui.Shared.Entities;

namespace Nemui.Infrastructure.Data.Configurations;

public class PlayerConfiguration : IEntityTypeConfiguration<Player>
{
    public void Configure(EntityTypeBuilder<Player> builder)
    {
        builder.ToTable(QuizConstants.TableNames.Players);

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Nickname)
            .HasMaxLength(QuizConstants.FieldLengths.PlayerNicknameMaxLength)
            .IsRequired();

        builder.Property(p => p.TotalScore)
            .HasDefaultValue(0);

        builder.Property(p => p.JoinedAt)
            .IsRequired();

        builder.Property(p => p.IsConnected)
            .HasDefaultValue(true);

        builder.Property(p => p.ConnectionId)
            .HasMaxLength(100);

        builder.HasIndex(p => p.GameSessionId)
            .HasDatabaseName(QuizConstants.Indexes.PlayerGameSessionIdIndex);
        builder.HasIndex(p => p.UserId)
            .HasDatabaseName(QuizConstants.Indexes.PlayerUserIdIndex);
        builder.HasIndex(p => new { p.GameSessionId, p.Nickname })
            .IsUnique();

        builder.HasIndex(p => p.JoinedAt);
        builder.HasIndex(p => p.CreatedAt);
        builder.HasIndex(p => p.IsDeleted);

        builder.HasOne(p => p.GameSession)
            .WithMany(gs => gs.Players)
            .HasForeignKey(p => p.GameSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(p => p.PlayerAnswers)
            .WithOne(pa => pa.Player)
            .HasForeignKey(pa => pa.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);

        // Global query filter for soft delete
        builder.HasQueryFilter(p => !p.IsDeleted && !p.GameSession.IsDeleted);
    }
}