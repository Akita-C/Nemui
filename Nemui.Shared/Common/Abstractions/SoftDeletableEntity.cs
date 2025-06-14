using Nemui.Shared.Common.Interfaces;

namespace Nemui.Shared.Common.Abstractions;

public class SoftDeletableEntity : AuditableEntity, ISoftDeletableEntity
{
    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
    public bool IsDeleted { get; set; }
}