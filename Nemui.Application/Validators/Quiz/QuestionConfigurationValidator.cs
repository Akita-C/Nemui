using System.Text.Json;
using FluentValidation;
using Nemui.Shared.DTOs.Quiz;
using Nemui.Shared.Enums;

namespace Nemui.Application.Validators.Quiz;

public static class QuestionConfigurationValidator
{
    public static bool IsValidConfiguration(QuestionType questionType, string? configuration)
    {
        if (string.IsNullOrEmpty(configuration)) return false;

        try
        {
            return questionType switch
            {
                QuestionType.MultipleChoice => ValidateMultipleChoiceConfig(configuration),
                QuestionType.TrueFalse => ValidateTrueFalseConfig(configuration),
                QuestionType.FillInTheBlank => ValidateFillInTheBlankConfig(configuration),
                QuestionType.Matching => ValidateMatchingConfig(configuration),
                QuestionType.Ordering => ValidateOrderingConfig(configuration),
                _ => false
            };
        }
        catch
        {
            return false;
        }
    }

    private static bool ValidateMultipleChoiceConfig(string configuration)
    {
        var config = JsonSerializer.Deserialize<MultipleChoiceConfig>(configuration);
        if (config == null) return false;

        // Must have at least 2 options
        if (config.Options.Count < 2) return false;

        // Must have at least 1 correct answer
        if (config.CorrectAnswerIndices.Count == 0) return false;

        // Correct answer indices must be valid
        return config.CorrectAnswerIndices.All(index => 
            index >= 0 && index < config.Options.Count);
    }

    private static bool ValidateTrueFalseConfig(string configuration)
    {
        var config = JsonSerializer.Deserialize<TrueFalseConfig>(configuration);
        return config != null;
    }

    private static bool ValidateFillInTheBlankConfig(string configuration)
    {
        var config = JsonSerializer.Deserialize<FillInTheBlankConfig>(configuration);
        if (config == null) return false;

        // Must have text with blanks
        if (string.IsNullOrEmpty(config.TextWithBlanks)) return false;

        // Must have at least 1 blank
        if (config.Blanks.Count == 0) return false;

        // Each blank must have at least 1 accepted answer
        return config.Blanks.All(blank => blank.AcceptedAnswers.Count > 0);
    }

    private static bool ValidateMatchingConfig(string configuration)
    {
        var config = JsonSerializer.Deserialize<MatchingConfig>(configuration);
        if (config == null) return false;

        // Must have equal number of left and right items
        if (config.LeftItems.Count != config.RightItems.Count) return false;

        // Must have at least 2 pairs
        if (config.LeftItems.Count < 2) return false;

        // Must have correct matches for all items
        if (config.CorrectMatches.Count != config.LeftItems.Count) return false;

        // All correct matches must reference valid items
        return config.CorrectMatches.All(match =>
            config.LeftItems.Any(item => item.Id == match.LeftId) &&
            config.RightItems.Any(item => item.Id == match.RightId));
    }

    private static bool ValidateOrderingConfig(string configuration)
    {
        var config = JsonSerializer.Deserialize<OrderingConfig>(configuration);
        if (config == null) return false;

        // Must have at least 2 items
        if (config.Items.Count < 2) return false;

        // Correct order must match number of items
        if (config.CorrectOrder.Count != config.Items.Count) return false;

        // All items in correct order must exist in items list
        return config.CorrectOrder.All(orderId =>
            config.Items.Any(item => item.Id == orderId));
    }
} 