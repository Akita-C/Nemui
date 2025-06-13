using Microsoft.AspNetCore.Http;

namespace Nemui.Shared.DTOs.Quiz;

public class CreateQuizRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public IFormFile? Thumbnail { get; set; }
    public bool IsPublic { get; set; } = false;
    public string? Category { get; set; }
    public string? Tags { get; set; }
    public int EstimatedDurationMinutes { get; set; } = 0;
}