using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nemui.Shared.Constants;
using Nemui.Shared.Entities;
using Nemui.Shared.Enums;

namespace Nemui.Infrastructure.Data.Configurations;

public class QuestionConfiguration : IEntityTypeConfiguration<Question>
{
    public void Configure(EntityTypeBuilder<Question> builder)
    {
        builder.ToTable(QuizConstants.TableNames.Questions);

        builder.HasKey(q => q.Id);

        builder.Property(q => q.Content)
            .HasMaxLength(QuizConstants.FieldLengths.QuestionContentMaxLength)
            .IsRequired();

        builder.Property(q => q.QuestionType)
            .HasConversion<int>()
            .HasDefaultValue(QuestionType.MultipleChoice)
            .HasSentinel(QuestionType.Unspecified);

        builder.Property(q => q.TimeLimitInSeconds)
            .HasDefaultValue(QuizConstants.Defaults.DefaultQuestionTimeLimit);

        builder.Property(q => q.Points)
            .HasDefaultValue(QuizConstants.Defaults.DefaultQuestionPoints);

        builder.Property(q => q.ImageUrl)
            .HasMaxLength(DatabaseConstants.FieldLengths.UrlMaxLength);

        builder.Property(q => q.ImagePublicId)
            .HasMaxLength(DatabaseConstants.FieldLengths.CloudinaryPublicIdMaxLength);

        builder.Property(q => q.Configuration)
            .HasColumnType("jsonb")
            .HasMaxLength(QuizConstants.FieldLengths.ConfigurationMaxLength);

        builder.Property(q => q.Explanation)
            .HasMaxLength(QuizConstants.FieldLengths.QuestionExplanationMaxLength);

        builder.HasIndex(q => q.QuizId)
            .HasDatabaseName(QuizConstants.Indexes.QuestionQuizIdIndex);
        builder.HasIndex(q => q.QuestionType);
        builder.HasIndex(q => q.CreatedAt);
        builder.HasIndex(q => q.IsDeleted);

        builder.HasOne(q => q.Quiz)
            .WithMany(quiz => quiz.Questions)
            .HasForeignKey(q => q.QuizId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(q => q.PlayerAnswers)
            .WithOne(pa => pa.Question)
            .HasForeignKey(pa => pa.QuestionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(q => !q.IsDeleted && !q.Quiz.IsDeleted);
    }
}