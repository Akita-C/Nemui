using Nemui.Shared.Common.Abstractions;

namespace Nemui.Shared.Entities;

public class Player : SoftDeletableEntity
{
    public string Nickname { get; set; } = string.Empty;
    public int TotalScore { get; set; } = 0;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public bool IsConnected { get; set; } = true;
    public string? ConnectionId { get; set; }
    
    public Guid GameSessionId { get; set; }
    public virtual GameSession GameSession { get; set; } = null!;
    public Guid? UserId { get; set; }
    public virtual User? User { get; set; }
    
    public virtual ICollection<PlayerAnswer> PlayerAnswers { get; set; } = new List<PlayerAnswer>();
}