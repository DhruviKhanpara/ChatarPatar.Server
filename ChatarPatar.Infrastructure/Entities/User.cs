using ChatarPatar.Infrastructure.Entities.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatarPatar.Infrastructure.Entities;

public class User : BaseEntity
{
    #region Table References
    [ForeignKey(nameof(AvatarFileId))]
    public FileEntity? AvatarFile { get; set; }
    [ForeignKey(nameof(DeletedBy))]
    public User? DeletedByUser { get; set; }
    #endregion

    public string Email { get; set; } = null!;
    public string Username { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;

    public Guid? AvatarFileId { get; set; }
    public string? Bio { get; set; }
    public bool IsEmailVerified { get; set; }

    // Soft delete
    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Guid? DeletedBy { get; set; }
    public DateTime? DeletedAt { get; set; }
}