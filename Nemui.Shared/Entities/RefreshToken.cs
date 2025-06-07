using System.ComponentModel.DataAnnotations;

namespace Nemui.Shared.Entities;

public class RefreshToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RevokedAt { get; set; }
    public bool IsExpired(DateTime? currentTime = null) => (currentTime ?? DateTime.UtcNow) >= ExpiresAt;
    public bool IsRevoked => RevokedAt != null;
    public bool IsActive(DateTime? currentTime = null) => !IsRevoked && !IsExpired(currentTime);

    public Guid UserId { get; set; }
    public virtual User User { get; set; } = null!;
}