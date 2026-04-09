using ChatarPatar.Common.Enums;
using ChatarPatar.Infrastructure.Entities.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatarPatar.Infrastructure.Entities;

public class OtpVerification : BaseEntity
{
    #region Table References
    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;
    #endregion

    public Guid UserId { get; set; }
    
    public string OtpHash { get; set; } = null!;
    public OtpPurposeEnum Purpose { get; set; }

    public DateTime ExpiresAt { get; set; }

    public bool IsUsed { get; set; }
    public DateTime? UsedAt { get; set; }

    public string? IPAddress { get; set; }

    public DateTime CreatedAt { get; set; }
}
