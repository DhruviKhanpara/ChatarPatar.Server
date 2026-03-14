using ChatarPatar.Common.AppExceptions.CustomExceptions;

namespace ChatarPatar.Common.Enums;

public enum FileFolderEnum
{
    Avatar,
    OrgLogo,
    TeamIcon,
    MessageImage,
    MessageVideo,
    MessageAudio,
    MessageFile
}

public static class FileFolderExtensions
{
    private const string Root = "app";

    public static string BuildFolder(this FileFolderEnum folder)
    {
        var now = DateTime.UtcNow;

        return folder switch
        {
            FileFolderEnum.Avatar => $"{Root}/profile/users",
            FileFolderEnum.OrgLogo => $"{Root}/profile/organizations",
            FileFolderEnum.TeamIcon => $"{Root}/profile/teams",
            FileFolderEnum.MessageImage => $"{Root}/messages/images/{now:yyyy/MM}",
            FileFolderEnum.MessageVideo => $"{Root}/messages/videos/{now:yyyy/MM}",
            FileFolderEnum.MessageAudio => $"{Root}/messages/audio/{now:yyyy/MM}",
            FileFolderEnum.MessageFile => $"{Root}/messages/files/{now:yyyy/MM}",
            _ => $"{Root}/misc"
        };
    }

    public static string BuildPublicId(this FileFolderEnum folder, Guid scopeId)
    {
        return folder switch
        {
            FileFolderEnum.Avatar => $"user_{scopeId}_avatar",
            FileFolderEnum.OrgLogo => $"org_{scopeId}_logo",
            FileFolderEnum.TeamIcon => $"team_{scopeId}_icon",
            _ => throw new AppException("Not define folder type")
        };
    }
}