namespace Nemui.Shared.DTOs.Quiz;

public class UpdateQuestionBulkItem
{
    public Guid QuestionId { get; set; }
    public UpdateQuestionRequest UpdateRequest { get; set; } = null!;
}