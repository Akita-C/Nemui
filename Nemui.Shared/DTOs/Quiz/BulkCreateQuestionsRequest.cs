using Nemui.Shared.Enums;

namespace Nemui.Shared.DTOs.Quiz;

public class BulkCreateQuestionsRequest
{
    public Guid QuizId { get; set; }
    public List<CreateQuestionItem> Questions { get; set; } = [];
}

public class CreateQuestionItem
{
    public string Content { get; set; } = string.Empty;
    public QuestionType QuestionType { get; set; } = QuestionType.MultipleChoice;
    public int TimeLimitInSeconds { get; set; } = 30;
    public int Points { get; set; } = 100;
    public string? ImageBase64 { get; set; } // For bulk upload, use base64
    public string? ImageFileName { get; set; }
    public string? Configuration { get; set; }
    public string? Explanation { get; set; }
    public int Order { get; set; } // Để sắp xếp thứ tự
} 