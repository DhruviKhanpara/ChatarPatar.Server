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
public enum PresenceStatusEnum : byte
{
    Offline = 0,
    Online = 1,
    Away = 2
}

public enum CustomPresenceStatusEnum : byte
{
    Active = 1,
    Busy = 2,
    DoNotDisturb = 3,
    BeRightBack = 4,
    AppearAway = 5,
    AppearOffline = 6
}