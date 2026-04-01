namespace ChatarPatar.Common.Enums;

/// <summary>
/// IMPORTANT:
/// These enum values are persisted in the database using `HasConversion<byte>()`.
/// 
/// Do NOT change the order unless you also update the corresponding
/// database records and constraints.
///
/// Changing names without DB sync will break data consistency.
/// </summary>
public enum NotificationTypeEnum
{
    Mention = 1,
    ThreadReply = 2,
    Reaction = 3,
    DirectMessage = 4,
    GroupMessage = 5,
    AddedToTeam = 6,
    AddedToChannel = 7,
    AddedToGroup = 8
}
