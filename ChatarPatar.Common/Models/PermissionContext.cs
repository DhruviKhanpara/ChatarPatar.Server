namespace ChatarPatar.Common.Models;

public record PermissionContext(
    Guid UserId,
    Guid OrgId,
    Guid? TeamId,       // null if checking org-level action
    Guid? ChannelId,    // null if not channel-scoped
    Guid? ConversationId // null if not conversation-scoped
);
