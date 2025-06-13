using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nemui.Shared.Constants;
using Nemui.Shared.Entities;

namespace Nemui.Infrastructure.Data.Configurations;

public class PlayerAnswerConfiguration : IEntityTypeConfiguration<PlayerAnswer>
{
    public void Configure(EntityTypeBuilder<PlayerAnswer> builder)
    {
        builder.ToTable(QuizConstants.TableNames.PlayerAnswers);

        builder.HasKey(pa => pa.Id);

        builder.Property(pa => pa.Answer)
            .HasColumnType("jsonb")
            .HasMaxLength(QuizConstants.FieldLengths.PlayerAnswerMaxLength);

        builder.Property(pa => pa.TimeTakenInSeconds)
            .HasDefaultValue(0);

        builder.Property(pa => pa.PointsEarned)
            .HasDefaultValue(0);

        builder.Property(pa => pa.IsCorrect)
            .HasDefaultValue(false);

        builder.Property(pa => pa.AnsweredAt)
            .IsRequired();

        builder.HasIndex(pa => pa.PlayerId)
            .HasDatabaseName(QuizConstants.Indexes.PlayerAnswerPlayerIdIndex);
        builder.HasIndex(pa => pa.QuestionId)
            .HasDatabaseName(QuizConstants.Indexes.PlayerAnswerQuestionIdIndex);
        builder.HasIndex(pa => pa.GameSessionId)
            .HasDatabaseName(QuizConstants.Indexes.PlayerAnswerGameSessionIdIndex);
        builder.HasIndex(pa => new { pa.PlayerId, pa.QuestionId })
            .IsUnique();

        builder.HasIndex(pa => pa.AnsweredAt);
        builder.HasIndex(pa => pa.CreatedAt);
        builder.HasIndex(pa => pa.IsDeleted);

        builder.HasOne(pa => pa.Player)
            .WithMany(p => p.PlayerAnswers)
            .HasForeignKey(pa => pa.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pa => pa.Question)
            .WithMany(q => q.PlayerAnswers)
            .HasForeignKey(pa => pa.QuestionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(pa => pa.GameSession)
            .WithMany(gs => gs.PlayerAnswers)
            .HasForeignKey(pa => pa.GameSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(pa => !pa.IsDeleted && !pa.Player.IsDeleted && !pa.GameSession.IsDeleted);
    }
}