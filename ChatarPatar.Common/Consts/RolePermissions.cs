using ChatarPatar.Common.Enums;

namespace ChatarPatar.Common.Consts;

public static class RolePermissions
{
    public static readonly IReadOnlyDictionary<OrganizationRoleEnum, HashSet<string>> OrganizationRolePermissions = Organization;
    public static readonly IReadOnlyDictionary<TeamRoleEnum, HashSet<string>> TeamRolePermissions = Team;
    public static readonly IReadOnlyDictionary<ChannelRoleEnum, HashSet<string>> ChannelRolePermissions = Channel;
    public static readonly IReadOnlyDictionary<ConversationParticipantRoleEnum, HashSet<string>>  ConversationRolePermissions = Conversation;

    #region Private section

    private static readonly Dictionary<OrganizationRoleEnum, HashSet<string>> Organization =
    new()
    {
        [OrganizationRoleEnum.OrgOwner] = new() { "*" },

        [OrganizationRoleEnum.OrgAdmin] = new()
        {
            Permissions.ORG_SETTINGS_EDIT,
            Permissions.ORG_BILLING,
            Permissions.ORG_AUDIT_LOG_VIEW,
            Permissions.ORG_MEMBERS_INVITE,
            Permissions.ORG_MEMBERS_REMOVE,
            Permissions.ORG_MEMBERS_ROLE_CHANGE,
            Permissions.ORG_TEAMS_CREATE,
            Permissions.ORG_TEAMS_DELETE,
            Permissions.TEAM_ARCHIVE,
            Permissions.TEAM_SETTINGS_EDIT,
            Permissions.TEAM_DELETE,
            Permissions.TEAM_MEMBERS_INVITE,
            Permissions.TEAM_MEMBERS_KICK,
            Permissions.TEAM_MEMBERS_ROLE_CHANGE,
            Permissions.TEAM_CHANNELS_CREATE,
            Permissions.TEAM_CHANNELS_DELETE,
            Permissions.TEAM_CHANNELS_ARCHIVE,
            Permissions.CHANNEL_MEMBERS_ROLE_CHANGE,
            Permissions.MESSAGE_DELETE_ANY,
            Permissions.MESSAGE_PIN,
        },

        [OrganizationRoleEnum.OrgMember] = new()
        {
            Permissions.ORG_TEAMS_CREATE,
        },

        [OrganizationRoleEnum.OrgGuest] = new()
        {
        }
    };

    private static readonly Dictionary<TeamRoleEnum, HashSet<string>> Team =
    new()
    {
        [TeamRoleEnum.TeamAdmin] = new()
        {
            Permissions.TEAM_ARCHIVE,
            Permissions.TEAM_SETTINGS_EDIT,
            Permissions.TEAM_DELETE,
            Permissions.TEAM_MEMBERS_INVITE,
            Permissions.TEAM_MEMBERS_KICK,
            Permissions.TEAM_MEMBERS_ROLE_CHANGE,
            Permissions.TEAM_CHANNELS_CREATE,
            Permissions.TEAM_CHANNELS_DELETE,
            Permissions.TEAM_CHANNELS_ARCHIVE,
            Permissions.CHANNEL_MEMBERS_ROLE_CHANGE,
            Permissions.CHANNEL_SETTINGS_EDIT,
            Permissions.CHANNEL_MEMBERS_ADD,
            Permissions.CHANNEL_MEMBERS_REMOVE,
            Permissions.MESSAGE_SEND,
            Permissions.MESSAGE_EDIT_OWN,
            Permissions.MESSAGE_DELETE_OWN,
            Permissions.MESSAGE_DELETE_ANY,
            Permissions.MESSAGE_PIN,
            Permissions.MESSAGE_REACT,
            Permissions.MESSAGE_THREAD_REPLY,
        },

        [TeamRoleEnum.TeamMember] = new()
        {
            Permissions.TEAM_MEMBERS_INVITE,
            Permissions.TEAM_CHANNELS_CREATE,
            Permissions.MESSAGE_SEND,
            Permissions.MESSAGE_EDIT_OWN,
            Permissions.MESSAGE_DELETE_OWN,
            Permissions.MESSAGE_REACT,
            Permissions.MESSAGE_THREAD_REPLY,
        },

        [TeamRoleEnum.TeamGuest] = new()
        {
            Permissions.MESSAGE_EDIT_OWN,
            Permissions.MESSAGE_THREAD_REPLY,
        }
    };

    private static readonly Dictionary<ChannelRoleEnum, HashSet<string>> Channel =
    new()
    {
        [ChannelRoleEnum.ChannelModerator] = new()
        {
            Permissions.CHANNEL_SETTINGS_EDIT,
            Permissions.CHANNEL_MEMBERS_ADD,
            Permissions.CHANNEL_MEMBERS_REMOVE,
            Permissions.CHANNEL_MEMBERS_ROLE_CHANGE,
            Permissions.MESSAGE_SEND,
            Permissions.MESSAGE_EDIT_OWN,
            Permissions.MESSAGE_DELETE_OWN,
            Permissions.MESSAGE_DELETE_ANY,
            Permissions.MESSAGE_PIN,
            Permissions.MESSAGE_REACT,
            Permissions.MESSAGE_THREAD_REPLY,
        },

        [ChannelRoleEnum.ChannelMember] = new()
        {
            Permissions.MESSAGE_SEND,
            Permissions.MESSAGE_EDIT_OWN,
            Permissions.MESSAGE_DELETE_OWN,
            Permissions.MESSAGE_REACT,
            Permissions.MESSAGE_THREAD_REPLY,
        },

        [ChannelRoleEnum.ChannelReadOnly] = new()
        {
        }
    };

    private static readonly Dictionary<ConversationParticipantRoleEnum, HashSet<string>> Conversation =
    new()
    {
        [ConversationParticipantRoleEnum.GroupAdmin] = new()
        {
            Permissions.GROUP_SETTINGS_EDIT,
            Permissions.GROUP_MEMBERS_ADD,
            Permissions.GROUP_MEMBERS_REMOVE,
            Permissions.GROUP_MEMBERS_ROLE_CHANGE,
            Permissions.GROUP_DELETE,
            Permissions.MESSAGE_SEND,
            Permissions.MESSAGE_PIN,
            Permissions.MESSAGE_EDIT_OWN,
            Permissions.MESSAGE_DELETE_OWN,
            Permissions.MESSAGE_DELETE_ANY,
            Permissions.MESSAGE_REACT,
            Permissions.MESSAGE_THREAD_REPLY,
        },

        [ConversationParticipantRoleEnum.GroupMember] = new()
        {
            Permissions.MESSAGE_SEND,
            Permissions.MESSAGE_EDIT_OWN,
            Permissions.MESSAGE_DELETE_OWN,
            Permissions.MESSAGE_REACT,
            Permissions.MESSAGE_THREAD_REPLY,
        }
    };

    #endregion
}
