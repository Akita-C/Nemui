namespace Nemui.Shared.DTOs.Quiz;

public class QuizListRequest
{
    public string? Search { get; set; }
    public string? Category { get; set; }
    public bool? IsPublic { get; set; }
    public Guid? CreatorId { get; set; }
    public string? SortBy { get; set; } = "CreatedAt";
    public bool IsDescending { get; set; } = true;
    public int PageSize { get; set; } = 10;
    public string? Cursor { get; set; }
}