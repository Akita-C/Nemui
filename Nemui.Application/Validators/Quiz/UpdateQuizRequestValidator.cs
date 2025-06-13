using FluentValidation;
using Nemui.Shared.DTOs.Quiz;

namespace Nemui.Application.Validators.Quiz;

public class UpdateQuizRequestValidator : AbstractValidator<UpdateQuizRequest>
{
    public UpdateQuizRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .Length(1, 200).WithMessage("Title must be between 1 and 200 characters");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters");

        RuleFor(x => x.Category)
            .MaximumLength(100).WithMessage("Category must not exceed 100 characters");

        RuleFor(x => x.Tags)
            .MaximumLength(500).WithMessage("Tags must not exceed 500 characters");

        RuleFor(x => x.EstimatedDurationMinutes)
            .GreaterThanOrEqualTo(0).WithMessage("Estimated duration must be greater than or equal to 0")
            .LessThanOrEqualTo(1440).WithMessage("Estimated duration must not exceed 24 hours");

        RuleFor(x => x.Thumbnail)
            .Must(file => file is not { Length: > 5 * 1024 * 1024 })
            .WithMessage("Thumbnail file size must not exceed 5MB")
            .Must(file => file == null || IsValidImageType(file.ContentType))
            .WithMessage("Thumbnail must be an image file (jpg, jpeg, png, gif, webp)");
    }

    private static bool IsValidImageType(string contentType)
    {
        var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp" };
        return allowedTypes.Contains(contentType.ToLower());
    }
}