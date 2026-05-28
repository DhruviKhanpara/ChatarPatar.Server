namespace ChatarPatar.Common.Enums;

/// <summary>
/// IMPORTANT:
/// These enum values are persisted in the database using `.ToString().ToLower()`.
/// Do NOT rename values without a matching DB migration.
/// 
/// Changing names without DB sync will break data consistency.
/// </summary>
public enum FileStatusEnum
{
    /// <summary>
    /// Attachment uploaded but not yet linked to a message.
    /// Row has no scope and will be deleted by the cleanup job after ExpiresAt.
    /// </summary>
    Pending = 1,

    /// <summary>
    /// File is linked to its owner entity (user, org, team, channel, conversation, or message). Scope column is set. ExpiresAt is NULL.
    /// </summary>
    Attached = 2,
}