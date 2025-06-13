using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nemui.Shared.Constants;
using Nemui.Shared.Entities;

namespace Nemui.Infrastructure.Data.Configurations;

public class GameSessionConfiguration : IEntityTypeConfiguration<GameSession>
{
    public void Configure(EntityTypeBuilder<GameSession> builder)
    {
        builder.ToTable(QuizConstants.TableNames.GameSessions);

        builder.HasKey(gs => gs.Id);

        builder.Property(gs => gs.SessionCode)
            .HasMaxLength(QuizConstants.FieldLengths.SessionCodeLength)
            .IsRequired();

        builder.Property(gs => gs.StartedAt)
            .IsRequired();

        builder.Property(gs => gs.Settings)
            .HasColumnType("jsonb");

        // Indexes
        builder.HasIndex(gs => gs.SessionCode)
            .IsUnique()
            .HasDatabaseName(QuizConstants.Indexes.GameSessionCodeIndex);

        builder.HasIndex(gs => gs.HostId)
            .HasDatabaseName(QuizConstants.Indexes.GameSessionHostIdIndex);

        builder.HasIndex(gs => gs.QuizId);
        builder.HasIndex(gs => gs.StartedAt);
        builder.HasIndex(gs => gs.CreatedAt);
        builder.HasIndex(gs => gs.IsDeleted);

        builder.HasOne(gs => gs.Quiz)
            .WithMany(q => q.GameSessions)
            .HasForeignKey(gs => gs.QuizId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(gs => gs.Host)
            .WithMany()
            .HasForeignKey(gs => gs.HostId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(gs => gs.Players)
            .WithOne(p => p.GameSession)
            .HasForeignKey(p => p.GameSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(gs => gs.PlayerAnswers)
            .WithOne(pa => pa.GameSession)
            .HasForeignKey(pa => pa.GameSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(gs => !gs.IsDeleted && !gs.Quiz.IsDeleted);
    }
}