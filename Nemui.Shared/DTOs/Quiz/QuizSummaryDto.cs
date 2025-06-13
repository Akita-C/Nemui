namespace Nemui.Shared.DTOs.Quiz;

public class QuizSummaryDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ThumbnailUrl { get; set; }
    public bool IsPublic { get; set; }
    public string? Category { get; set; }
    public int EstimatedDurationMinutes { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid CreatorId { get; set; }
    public string CreatorName { get; set; } = string.Empty;
    public int QuestionCount { get; set; }
    public Dictionary<string, string>? ThumbnailTransformations { get; set; }
}