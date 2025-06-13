using Nemui.Shared.Enums;

namespace Nemui.Shared.DTOs.Quiz;

public class QuestionListRequest
{
    public Guid? QuizId { get; set; }
    public string? Search { get; set; }
    public QuestionType? QuestionType { get; set; }
    public string? SortBy { get; set; } = "CreatedAt";
    public bool IsDescending { get; set; } = true;
    public int PageSize { get; set; } = 10;
    public string? Cursor { get; set; }
} 