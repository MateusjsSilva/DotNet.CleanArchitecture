namespace CleanArchitecture.Domain.Common;

public abstract class AuditableEntity : BaseEntity, ISoftDeletable
{
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    /// <summary>
    /// Marks the entity as deleted. Audit fields (DeletedAt, DeletedBy) are stamped
    /// by ApplicationDbContext.SetAuditFields() during SaveChangesAsync, exactly like
    /// CreatedAt/UpdatedBy — never set them here to keep audit logic in one place.
    /// </summary>
    public void SoftDelete()
    {
        if (IsDeleted) return;
        IsDeleted = true;
    }
}
