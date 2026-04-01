namespace ChatarPatar.Common.Helpers;

public sealed class CloudinaryPath
{
    private readonly List<string> _segments = new();

    private CloudinaryPath() { }

    private CloudinaryPath Add(string segment)
    {
        _segments.Add(segment);
        return this;
    }

    private static string DatePath()
    {
        return DateTime.UtcNow.ToString("yyyy/MM");
    }

    public override string ToString()
    {
        return string.Join("/", _segments);
    }

    #region Root Builders

    public static CloudinaryPath Organization(Guid orgId)
    {
        return new CloudinaryPath()
            .Add("app")
            .Add($"organizations/org_{orgId}");
    }

    public static CloudinaryPath Users()
    {
        return new CloudinaryPath()
            .Add("app")
            .Add("users");
    }

    public static CloudinaryPath Conversation(Guid conversationId)
    {
        return new CloudinaryPath()
            .Add("app")
            .Add($"conversations/conv_{conversationId}");
    }

    #endregion

    #region Entity Builders

    public CloudinaryPath Team(Guid teamId)
    {
        return Add($"teams/team_{teamId}");
    }

    public CloudinaryPath Channel(Guid channelId)
    {
        return Add($"channels/channel_{channelId}");
    }

    #endregion

    #region Static Folders

    public string Profile()
    {
        return $"{this}/profile";
    }

    public string Avatars()
    {
        return $"{this}/avatars";
    }

    #endregion

    #region Messages

    public CloudinaryPath Messages()
    {
        return Add("messages");
    }

    public string Images()
    {
        return $"{this}/images/{DatePath()}";
    }

    public string Videos()
    {
        return $"{this}/videos/{DatePath()}";
    }

    public string Audio()
    {
        return $"{this}/audio/{DatePath()}";
    }

    public string Files()
    {
        return $"{this}/files/{DatePath()}";
    }

    #endregion
}