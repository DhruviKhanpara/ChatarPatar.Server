using System.ComponentModel.DataAnnotations.Schema;

namespace ChatarPatar.Infrastructure.Entities.Common;

public abstract class AuditableEntity : BaseEntity
{
    #region Table References
    [ForeignKey(nameof(CreatedBy))]
    public User? CreatedByUser { get; set; }
    [ForeignKey(nameof(UpdatedBy))]
    public User? UpdatedByUser { get; set; }
    [ForeignKey(nameof(DeletedBy))]
    public User? DeletedByUser { get; set; }
    #endregion

    public bool IsDeleted { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? DeletedBy { get; set; }
    public DateTime? DeletedAt { get; set; }
}
