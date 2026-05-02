namespace ChatarPatar.Common.Consts;

public static class Permissions
{
    // ── Org-level ──────────────────────────────────────
    public const string ORG_SETTINGS_EDIT = "org:settings:edit";
    public const string ORG_BILLING = "org:billing";
    public const string ORG_AUDIT_LOG_VIEW = "org:auditlog:view";
    public const string ORG_MEMBERS_INVITE = "org:members:invite";
    public const string ORG_MEMBERS_REMOVE = "org:members:remove";
    public const string ORG_MEMBERS_ROLE_CHANGE = "org:members:role:change";
    public const string ORG_INVITES_MANAGE = "org:invites:manage";
    public const string ORG_TEAMS_CREATE = "org:teams:create";
    public const string ORG_TEAMS_DELETE = "org:teams:delete";

    // ── Team-level ─────────────────────────────────────
    public const string TEAM_ARCHIVE = "team:archive";
    public const string TEAM_SETTINGS_EDIT =  "team:settings:edit";
    public const string TEAM_DELETE =  "team:delete";
    public const string TEAM_MEMBERS_INVITE =  "team:members:invite";
    public const string TEAM_MEMBERS_KICK =  "team:members:kick";
    public const string TEAM_MEMBERS_ROLE_CHANGE =  "team:members:role:change";
    public const string TEAM_CHANNELS_CREATE =  "team:channels:create";
    public const string TEAM_CHANNELS_DELETE =  "team:channels:delete";
    public const string TEAM_CHANNELS_ARCHIVE = "team:channels:archive";
 
    // ── Channel-level ──────────────────────────────────
    public const string CHANNEL_SETTINGS_EDIT = "channel:settings:edit";
    public const string CHANNEL_MEMBERS_ADD = "channel:members:add";
    public const string CHANNEL_MEMBERS_REMOVE = "channel:members:remove";
    public const string CHANNEL_MEMBERS_ROLE_CHANGE = "channel:members:role:change";
 
    // ── Message-level ──────────────────────────────────
    public const string MESSAGE_SEND = "message:send";
    public const string MESSAGE_EDIT_OWN = "message:edit:own";
    public const string MESSAGE_DELETE_OWN = "message:delete:own";
    public const string MESSAGE_DELETE_ANY = "message:delete:any";
    public const string MESSAGE_PIN = "message:pin";
    public const string MESSAGE_REACT = "message:react";
    public const string MESSAGE_THREAD_REPLY = "message:thread:reply";

    // ── Group Chat-level ───────────────────────────────
    public const string GROUP_SETTINGS_EDIT = "groupchat:settings:edit";
    public const string GROUP_MEMBERS_ADD = "groupchat:members:add";
    public const string GROUP_MEMBERS_REMOVE = "groupchat:members:remove";
    public const string GROUP_MEMBERS_ROLE_CHANGE = "groupchat:members:role:change";
    public const string GROUP_DELETE = "groupchat:delete";
}
