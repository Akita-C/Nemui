using Nemui.Shared.Common.Abstractions;

namespace Nemui.Shared.Entities;

public class GameSession : SoftDeletableEntity
{
    public string SessionCode { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public string? Settings { get; set; }

    public Guid QuizId { get; set; }
    public virtual Quiz Quiz { get; set; } = null!;
    public Guid HostId { get; set; }
    public virtual User Host { get; set; } = null!;

    public virtual ICollection<Player> Players { get; set; } = null!;
    public virtual ICollection<PlayerAnswer> PlayerAnswers { get; set; } = new List<PlayerAnswer>();
    
    public bool IsCompleted => EndedAt.HasValue;
}
