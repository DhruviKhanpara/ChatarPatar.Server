using ChatarPatar.Common.AppExceptions.CustomExceptions;

namespace ChatarPatar.Common.Models;

public class FileFolderContext
{
    public Guid? OrgId { get; set; }
    public Guid? TeamId { get; set; }
    public Guid? ChannelId { get; set; }
    public Guid? ConversationId { get; set; }

    public Guid GetOrgId() =>
        OrgId ?? throw new InvalidDataAppException("OrgId required");

    public Guid GetTeamId() =>
        TeamId ?? throw new InvalidDataAppException("TeamId required");

    public Guid GetChannelId() =>
        ChannelId ?? throw new InvalidDataAppException("ChannelId required");

    public Guid GetConversationId() =>
        ConversationId ?? throw new InvalidDataAppException("ConversationId required");
}
