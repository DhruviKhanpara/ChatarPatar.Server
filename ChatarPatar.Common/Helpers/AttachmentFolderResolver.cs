using ChatarPatar.Common.Enums;

namespace ChatarPatar.Common.Helpers;

/// <summary>
/// Resolves the correct Cloudinary folder for a message attachment upload,
/// based on where the message will live and what type of file it is.
/// </summary>
public static class AttachmentFolderResolver
{
    /// <summary>
    /// Resolves the upload folder for a channel attachment.
    /// </summary>
    public static string ForChannel(Guid orgId, Guid teamId, Guid channelId, FileTypeEnum fileType)
    {
        var basePath = CloudinaryPath
            .Organization(orgId)
            .Team(teamId)
            .Channel(channelId)
            .Messages();

        return ResolveTypeFolder(basePath, fileType);
    }

    /// <summary>
    /// Resolves the upload folder for a conversation attachment.
    /// </summary>
    public static string ForConversation(Guid conversationId, FileTypeEnum fileType)
    {
        var basePath = CloudinaryPath
            .Conversation(conversationId)
            .Messages();

        return ResolveTypeFolder(basePath, fileType);
    }

    #region Private Section

    private static string ResolveTypeFolder(CloudinaryPath messagesPath, FileTypeEnum fileType) =>
        fileType switch
        {
            FileTypeEnum.Image => messagesPath.Images(),
            FileTypeEnum.Video => messagesPath.Videos(),
            FileTypeEnum.Audio => messagesPath.Audio(),
            _ => messagesPath.Files(),   // Document, Code, Archive, Other
        };

    #endregion
}
