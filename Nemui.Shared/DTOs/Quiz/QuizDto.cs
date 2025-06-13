namespace Nemui.Shared.DTOs.Quiz;

public class QuizDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ThumbnailUrl { get; set; }
    public bool IsPublic { get; set; }
    public string? Category { get; set; }
    public string? Tags { get; set; }
    public int EstimatedDurationMinutes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid CreatorId { get; set; }
    public string CreatorName { get; set; } = string.Empty;
    public int QuestionCount { get; set; }
    public Dictionary<string, string>? ThumbnailTransformations { get; set; }
}