namespace ChatarPatar.Common.Enums;

/// <summary>
/// IMPORTANT:
/// These enum values are persisted in the database as NVARCHAR via .ToString().
///
/// Do NOT rename these values unless you also update the corresponding
/// database records and constraints.
/// </summary>
public enum NotificationTemplateTypeEnum
{
    Email = 1,
    Sms = 2,
    //Push = 3
}