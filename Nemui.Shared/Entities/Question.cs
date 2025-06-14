using System.Text.Json;
using Nemui.Shared.Common.Abstractions;
using Nemui.Shared.Enums;

namespace Nemui.Shared.Entities;

public class Question : SoftDeletableEntity
{
    public string Content { get; set; } = string.Empty;
    public QuestionType QuestionType { get; set; } = QuestionType.Unspecified;
    public int TimeLimitInSeconds { get; set; } = 30;
    public int Points { get; set; } = 100;
    public string? ImageUrl { get; set; }
    public string? ImagePublicId { get; set; }
    public string? Configuration { get; set; }
    public string? Explanation { get; set; }

    public Guid QuizId { get; set; }
    public virtual Quiz Quiz { get; set; } = null!;
    
    public virtual ICollection<PlayerAnswer> PlayerAnswers { get; set; } = new List<PlayerAnswer>();

    public T? GetConfiguration<T>() where T : class
    {
        if (string.IsNullOrEmpty(Configuration)) return null;

        try
        {
            return JsonSerializer.Deserialize<T>(Configuration);
        }
        catch
        {
            return null;
        }
    }

    public void SetConfiguration<T>(T config) where T : class
    {
        Configuration = JsonSerializer.Serialize(config);
    }
}