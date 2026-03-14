using ChatarPatar.Common.Enums;

namespace ChatarPatar.Common.Consts;

public static class AllowedMimeTypes
{
    public static readonly Dictionary<FileTypeEnum, string[]> MimeTypes = new()
    {
        [FileTypeEnum.Image] = new[]
        {
            "image/jpeg",
            "image/png",
            "image/gif",
            "image/webp"
        },

        [FileTypeEnum.Video] = new[]
        {
            "video/mp4",
            "video/quicktime",
            "video/x-msvideo",
            "video/x-matroska"
        },

        [FileTypeEnum.Audio] = new[]
        {
            "audio/mpeg",
            "audio/wav",
            "audio/aac"
        },

        [FileTypeEnum.Document] = new[]
        {
            "application/pdf",
            "application/msword",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "text/plain",
            "application/vnd.ms-excel",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        },

        [FileTypeEnum.Code] = new[]
        {
            "text/plain",
            "application/json",
            "application/xml",
            "text/html",
            "text/css",
            "application/javascript"
        },

        [FileTypeEnum.Archive] = new[]
        {
            "application/zip",
            "application/x-rar-compressed",
            "application/x-7z-compressed",
            "application/gzip"
        }
    };
}
