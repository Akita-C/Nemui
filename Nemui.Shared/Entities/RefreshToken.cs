using System.ComponentModel.DataAnnotations;
using Nemui.Shared.Common.Abstractions;

namespace Nemui.Shared.Entities;

public class RefreshToken : AuditableEntity
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    
    public bool IsExpired(DateTime? currentTime = null) => (currentTime ?? DateTime.UtcNow) >= ExpiresAt;
    public bool IsRevoked => RevokedAt != null;
    public bool IsActive(DateTime? currentTime = null) => !IsRevoked && !IsExpired(currentTime);

    public Guid UserId { get; set; }
    public virtual User User { get; set; } = null!;
}