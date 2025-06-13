using Nemui.Shared.Common.Abstractions;

namespace Nemui.Shared.Entities;

public class PlayerAnswer : SoftDeletableEntity
{
    public Guid PlayerId { get; set; }
    public virtual Player Player { get; set; } = null!;
    public Guid QuestionId { get; set; }
    public virtual Question Question { get; set; } = null!;
    public Guid GameSessionId { get; set; }
    public virtual GameSession GameSession { get; set; } = null!;
    
    public string? Answer { get; set; } 
    public int TimeTakenInSeconds { get; set; }
    public int PointsEarned { get; set; } = 0;
    public bool IsCorrect { get; set; } = false;
    public DateTime AnsweredAt { get; set; } = DateTime.UtcNow;
}