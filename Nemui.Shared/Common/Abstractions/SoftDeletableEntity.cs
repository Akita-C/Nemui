namespace Nemui.Shared.Common.Abstractions;

public class SoftDeletableEntity : AuditableEntity
{
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
    public bool IsDeleted { get; set; }
}