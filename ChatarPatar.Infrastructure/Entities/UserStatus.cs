using ChatarPatar.Common.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatarPatar.Infrastructure.Entities;

public class UserStatus
{
    #region Table References
    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;
    #endregion

    public Guid UserId { get; set; }

    public PresenceStatusEnum Status { get; set; }
    public CustomPresenceStatusEnum? CustomStatus { get; set; }

    public DateTime LastSeenAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}