namespace ChatarPatar.Common.Helpers;

public static class CloudinaryPublicId
{
    public static string UserAvatar(Guid userId)
        => $"user_{userId}_avatar";

    public static string OrgLogo(Guid orgId)
        => $"org_{orgId}_logo";

    public static string TeamIcon(Guid teamId)
        => $"team_{teamId}_icon";

    public static string ConversationLogo(Guid conversationId)
        => $"conversation_{conversationId}_logo";
}
