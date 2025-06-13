using Nemui.Shared.Enums;

namespace Nemui.Shared.DTOs.Quiz;

public class QuestionDto
{
    public Guid Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public QuestionType QuestionType { get; set; }
    public int TimeLimitInSeconds { get; set; }
    public int Points { get; set; }
    public string? ImageUrl { get; set; }
    public string? Configuration { get; set; }
    public string? Explanation { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid QuizId { get; set; }
    public string QuizTitle { get; set; } = string.Empty;
    public Dictionary<string, string>? ImageTransformations { get; set; }
} 