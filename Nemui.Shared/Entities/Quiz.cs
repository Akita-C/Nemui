using Nemui.Shared.Common.Abstractions;

namespace Nemui.Shared.Entities;

public class Quiz : SoftDeletableEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? ThumbnailPublicId { get; set; }
    public bool IsPublic { get; set; } = false;
    public string? Category { get; set; }
    public string? Tags { get; set; }
    public int EstimatedDurationMinutes { get; set; } = 0;

    public Guid CreatorId { get; set; }
    public virtual User Creator { get; set; } = null!;
    
    public virtual ICollection<Question> Questions { get; set; } = new List<Question>();
    public virtual ICollection<GameSession> GameSessions { get; set; } = new List<GameSession>();
}