using FluentValidation;
using Nemui.Shared.DTOs.Quiz;
using Nemui.Shared.Constants;

namespace Nemui.Application.Validators.Quiz;

public class BulkCreateQuestionsRequestValidator : AbstractValidator<BulkCreateQuestionsRequest>
{
    public BulkCreateQuestionsRequestValidator()
    {
        RuleFor(x => x.QuizId)
            .NotEmpty().WithMessage("Quiz ID is required");

        RuleFor(x => x.Questions)
            .NotEmpty().WithMessage("At least one question is required")
            .Must(x => x.Count <= 50).WithMessage("Cannot create more than 50 questions at once");

        RuleForEach(x => x.Questions).SetValidator(new CreateQuestionItemValidator());
    }
}

public class CreateQuestionItemValidator : AbstractValidator<CreateQuestionItem>
{
    public CreateQuestionItemValidator()
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

        RuleFor(x => x.ImageBase64)
            .Must(BeValidBase64Image).When(x => !string.IsNullOrEmpty(x.ImageBase64))
            .WithMessage("Invalid image format or size exceeds 10MB");

        RuleFor(x => x.Configuration)
            .NotEmpty().WithMessage("Configuration is required")
            .MaximumLength(QuizConstants.FieldLengths.ConfigurationMaxLength)
            .WithMessage($"Configuration must not exceed {QuizConstants.FieldLengths.ConfigurationMaxLength} characters")
            .Must((item, config) => QuestionConfigurationValidator.IsValidConfiguration(item.QuestionType, config))
            .WithMessage("Configuration format is invalid for the selected question type");

        RuleFor(x => x.Explanation)
            .MaximumLength(QuizConstants.FieldLengths.QuestionExplanationMaxLength)
            .WithMessage($"Explanation must not exceed {QuizConstants.FieldLengths.QuestionExplanationMaxLength} characters");

        RuleFor(x => x.Order)
            .GreaterThanOrEqualTo(0).WithMessage("Order must be greater than or equal to 0");
    }

    private bool BeValidBase64Image(string? base64)
    {
        if (string.IsNullOrEmpty(base64)) return true;

        try
        {
            var bytes = Convert.FromBase64String(base64);
            return bytes.Length <= 10 * 1024 * 1024; // 10MB limit
        }
        catch
        {
            return false;
        }
    }
} 