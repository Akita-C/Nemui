namespace Nemui.Shared.Common.Interfaces;

public interface ISoftDeletableEntity : IAuditableEntity
{
    DateTimeOffset? DeletedAt { get; set; }   
    string? DeletedBy { get; set; }
    bool IsDeleted { get; set; }
}