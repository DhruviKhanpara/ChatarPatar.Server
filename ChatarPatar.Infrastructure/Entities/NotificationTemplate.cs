using ChatarPatar.Common.Enums;
using ChatarPatar.Infrastructure.Entities.Common;
using System.ComponentModel.DataAnnotations;

namespace ChatarPatar.Infrastructure.Entities;

public class NotificationTemplate : BaseEntity
{
    /// <summary>
    /// Logical name matched via NotificationTemplateNames constants.
    /// Unique per (Name, TemplateType).
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Transport type — Email, Sms, ...
    /// Stored as NVARCHAR via enum ToString().
    /// </summary>
    public NotificationTemplateTypeEnum TemplateType { get; set; }

    /// <summary>
    /// Subject line with optional {{Placeholder}} tokens.
    /// Nullable — SMS/Push have no subject.
    /// </summary>
    public string? SubjectText { get; set; }

    /// <summary>
    /// Body with {{Placeholder}} tokens.
    /// HTML for Email, plain text for Sms/Push.
    /// </summary>
    public string BodyText { get; set; } = null!;

    public bool IsActive { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = null!;
}
