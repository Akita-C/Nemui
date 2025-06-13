using Microsoft.AspNetCore.Http;
using Nemui.Shared.Enums;

namespace Nemui.Shared.DTOs.Quiz;

public class CreateQuestionRequest
{
    public string Content { get; set; } = string.Empty;
    public QuestionType QuestionType { get; set; } = QuestionType.MultipleChoice;
    public int TimeLimitInSeconds { get; set; } = 30;
    public int Points { get; set; } = 100;
    public IFormFile? Image { get; set; }
    public string? Configuration { get; set; }
    public string? Explanation { get; set; }
    public Guid QuizId { get; set; }
} 