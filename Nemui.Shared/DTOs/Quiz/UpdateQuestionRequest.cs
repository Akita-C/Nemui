using Microsoft.AspNetCore.Http;
using Nemui.Shared.Enums;

namespace Nemui.Shared.DTOs.Quiz;

public class UpdateQuestionRequest
{
    public string Content { get; set; } = string.Empty;
    public QuestionType QuestionType { get; set; }
    public int TimeLimitInSeconds { get; set; }
    public int Points { get; set; }
    public IFormFile? Image { get; set; }
    public string? Configuration { get; set; }
    public string? Explanation { get; set; }
} 