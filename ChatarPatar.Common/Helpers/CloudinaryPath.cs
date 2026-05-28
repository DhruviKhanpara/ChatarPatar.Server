namespace ChatarPatar.Common.Helpers;

/// <summary>
/// Fluent builder for Cloudinary folder paths.
/// </summary>
public sealed class CloudinaryPath
{
    private readonly List<string> _segments = new();

    private CloudinaryPath() { }

    private CloudinaryPath Add(string segment)
    {
        _segments.Add(segment);
        return this;
    }

    private static string DatePath() =>
        DateTime.UtcNow.ToString("yyyy/MM");

    public override string ToString() =>
        string.Join("/", _segments);

    #region Root Builders

    /// <summary>
    /// app/organizations/org_{orgId}
    /// </summary>
    public static CloudinaryPath Organization(Guid orgId) =>
        new CloudinaryPath()
            .Add("app")
            .Add($"organizations")
            .Add($"org_{orgId}");

    /// <summary>
    /// app/users
    /// </summary>
    public static CloudinaryPath Users() =>
        new CloudinaryPath()
            .Add("app")
            .Add("users");

    /// <summary>
    /// app/conversations/conv_{conversationId}
    /// </summary>
    public static CloudinaryPath Conversation(Guid conversationId) =>
        new CloudinaryPath()
            .Add("app")
            .Add($"conversations")
            .Add($"conv_{conversationId}");

    #endregion

    #region Child Entity Builders

    /// <summary>
    /// .../teams/team_{teamId}
    /// </summary>
    public CloudinaryPath Team(Guid teamId) =>
        Add($"teams").Add($"team_{teamId}");

    /// <summary>
    /// .../channels/channel_{channelId}
    /// </summary>
    public CloudinaryPath Channel(Guid channelId) =>
        Add("channels").Add($"channel_{channelId}");

    /// <summary>
    /// .../messages
    /// </summary>
    public CloudinaryPath Messages() =>
        Add("messages");

    #endregion

    #region Terminal Folders
    // These return string (not CloudinaryPath) because nothing chains after them.

    /// <summary>
    /// .../profile
    /// Used for: org logo, team icon, conversation logo.
    /// </summary>
    public string Profile() => $"{this}/profile";

    /// <summary>
    /// .../avatars
    /// Used for: user avatar uploads
    /// </summary>
    public string Avatars() => $"{this}/avatars";

    /// <summary>
    /// .../images/yyyy/MM
    /// Used for: image message attachments.
    /// </summary>
    public string Images() => $"{this}/images/{DatePath()}";

    /// <summary>
    /// .../videos/yyyy/MM
    /// Used for: video message attachments.
    /// </summary>
    public string Videos() => $"{this}/videos/{DatePath()}";

    /// <summary>
    /// .../audio/yyyy/MM
    /// Used for: audio message attachments.
    /// </summary>
    public string Audio() => $"{this}/audio/{DatePath()}";

    /// <summary>
    /// .../files/yyyy/MM
    /// Used for: document, code, archive message attachments.
    /// </summary>
    public string Files() => $"{this}/files/{DatePath()}";

    #endregion
}