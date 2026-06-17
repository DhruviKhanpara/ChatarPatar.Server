using ChatarPatar.Common.Enums;

namespace ChatarPatar.Application.DTOs.Conversation;

public class ConversationDto
{
    public Guid Id { get; set; }
    public ConversationTypeEnum Type { get; set; }

    // Group only
    public string? Name { get; set; }
    public string? LogoThumbnailUrl { get; set; }
    public int ParticipantCount { get; set; }

    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// For Direct conversations: the other person's info.
    /// Null for Group conversations.
    /// </summary>
    public DirectPeerDto? Peer { get; set; }

    /// <summary>
    /// Caller's role — only meaningful for Group (GroupAdmin / GroupMember).
    /// Null for Direct.
    /// </summary>
    public ConversationParticipantRoleEnum? Role { get; set; }

    /// <summary>
    /// Caller's joining date — only meaningful for Group (GroupAdmin / GroupMember).
    /// Null for Direct.
    /// </summary>
    public DateTime? JoinedAt { get; set; }

    public DateTime? LastMessageAt { get; set; }
    public int UnreadCount { get; set; }
    public int MentionCount { get; set; }
}
