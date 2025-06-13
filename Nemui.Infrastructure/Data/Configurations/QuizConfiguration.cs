using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nemui.Shared.Constants;
using Nemui.Shared.Entities;

namespace Nemui.Infrastructure.Data.Configurations;

public class QuizConfiguration : IEntityTypeConfiguration<Quiz>
{
    public void Configure(EntityTypeBuilder<Quiz> builder)
    {
        builder.ToTable(QuizConstants.TableNames.Quizzes);

        builder.HasKey(q => q.Id);

        builder.Property(q => q.Title)
            .HasMaxLength(QuizConstants.FieldLengths.QuizTitleMaxLength)
            .IsRequired();

        builder.Property(q => q.Description)
            .HasMaxLength(QuizConstants.FieldLengths.QuizDescriptionMaxLength);

        builder.Property(q => q.ThumbnailUrl)
            .HasMaxLength(DatabaseConstants.FieldLengths.UrlMaxLength);

        builder.Property(q => q.ThumbnailPublicId)
            .HasMaxLength(DatabaseConstants.FieldLengths.CloudinaryPublicIdMaxLength);

        builder.Property(q => q.Category)
            .HasMaxLength(QuizConstants.FieldLengths.QuizCategoryMaxLength);

        builder.Property(q => q.Tags)
            .HasColumnType("jsonb");

        builder.Property(q => q.IsPublic)
            .HasDefaultValue(false);

        builder.Property(q => q.EstimatedDurationMinutes)
            .HasDefaultValue(0);

        // Indexes
        builder.HasIndex(q => q.CreatorId)
            .HasDatabaseName(QuizConstants.Indexes.QuizCreatorIdIndex);

        builder.HasIndex(q => q.IsPublic)
            .HasDatabaseName(QuizConstants.Indexes.QuizIsPublicIndex);

        builder.HasIndex(q => q.Category)
            .HasDatabaseName(QuizConstants.Indexes.QuizCategoryIndex);

        builder.HasIndex(q => q.CreatedAt);
        builder.HasIndex(q => q.IsDeleted);

        // Relationships
        builder.HasOne(q => q.Creator)
            .WithMany()
            .HasForeignKey(q => q.CreatorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(q => q.Questions)
            .WithOne(qu => qu.Quiz)
            .HasForeignKey(qu => qu.QuizId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(q => q.GameSessions)
            .WithOne(gs => gs.Quiz)
            .HasForeignKey(gs => gs.QuizId)
            .OnDelete(DeleteBehavior.Restrict);

        // Global query filter for soft delete
        builder.HasQueryFilter(q => !q.IsDeleted);
    }
}