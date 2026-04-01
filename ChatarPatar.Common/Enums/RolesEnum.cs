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
public enum OrganizationRoleEnum
{
    OrgOwner = 1,
    OrgAdmin = 2,
    OrgMember = 3,
    OrgGuest = 4
}

public enum TeamRoleEnum
{
    TeamAdmin = 1,
    TeamMember = 2,
    TeamGuest = 3
}

public enum ChannelRoleEnum
{
    ChannelModerator = 1,
    ChannelMember = 2,
    ChannelReadOnly = 3
}

public enum ConversationParticipantRoleEnum
{
    GroupAdmin = 1,
    GroupMember = 2
}