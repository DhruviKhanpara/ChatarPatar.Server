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
public enum ChannelTypeEnum
{
    Text = 1,
    Announcement = 2
}
