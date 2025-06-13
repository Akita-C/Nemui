using FluentValidation;
using Nemui.Shared.DTOs.Quiz;
using Nemui.Shared.Constants;

namespace Nemui.Application.Validators.Quiz;

public class CreateQuestionRequestValidator : AbstractValidator<CreateQuestionRequest>
{
    public CreateQuestionRequestValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Question content is required")
            .Length(1, QuizConstants.FieldLengths.QuestionContentMaxLength)
            .WithMessage($"Question content must be between 1 and {QuizConstants.FieldLengths.QuestionContentMaxLength} characters");

        RuleFor(x => x.QuestionType)
            .IsInEnum().WithMessage("Invalid question type");

        RuleFor(x => x.TimeLimitInSeconds)
            .GreaterThan(0).WithMessage("Time limit must be greater than 0")
            .LessThanOrEqualTo(600).WithMessage("Time limit must not exceed 10 minutes");

        RuleFor(x => x.Points)
            .GreaterThan(0).WithMessage("Points must be greater than 0")
            .LessThanOrEqualTo(1000).WithMessage("Points must not exceed 1000");

        RuleFor(x => x.Image)
            .Must(file => file is not { Length: > 10 * 1024 * 1024 })
            .WithMessage("Image file size must not exceed 10MB")
            .Must(file => file == null || IsValidImageType(file.ContentType))
            .WithMessage("Image must be an image file (jpg, jpeg, png, gif, webp)");

        // Configuration validation based on question type
        RuleFor(x => x.Configuration)
            .NotEmpty().WithMessage("Configuration is required")
            .MaximumLength(QuizConstants.FieldLengths.ConfigurationMaxLength)
            .WithMessage($"Configuration must not exceed {QuizConstants.FieldLengths.ConfigurationMaxLength} characters")
            .Must((request, config) => QuestionConfigurationValidator.IsValidConfiguration(request.QuestionType, config))
            .WithMessage("Configuration format is invalid for the selected question type");

        RuleFor(x => x.Explanation)
            .MaximumLength(QuizConstants.FieldLengths.QuestionExplanationMaxLength)
            .WithMessage($"Explanation must not exceed {QuizConstants.FieldLengths.QuestionExplanationMaxLength} characters");

        RuleFor(x => x.QuizId)
            .NotEmpty().WithMessage("Quiz ID is required");
    }

    private static bool IsValidImageType(string contentType)
    {
        var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp" };
        return allowedTypes.Contains(contentType.ToLower());
    }
} 