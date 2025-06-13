namespace Nemui.Shared.DTOs.Quiz;

public class BulkCreateQuestionsResponse
{
    public List<QuestionDto> SuccessfulQuestions { get; set; } = [];
    public List<QuestionCreationError> FailedQuestions { get; set; } = [];
    public int TotalProcessed { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
}

public class QuestionCreationError
{
    public int Order { get; set; }
    public string Content { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = [];
} 