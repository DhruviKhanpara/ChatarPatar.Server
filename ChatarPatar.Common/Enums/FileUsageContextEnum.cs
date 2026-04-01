namespace ChatarPatar.Common.Enums;

/// <summary>
/// IMPORTANT:
/// These enum values are persisted in the database using `.ToString().ToLower()`.
/// 
/// Do NOT rename these values unless you also update the corresponding
/// database records and constraints.
///
/// Changing names without DB sync will break data consistency.
/// </summary>
public enum FileUsageContextEnum
{
    Avatar = 1,
    Org_Logo = 2,
    Team_Icon = 3,
    Conversation_Logo = 4,
    Attachment = 5,
}
