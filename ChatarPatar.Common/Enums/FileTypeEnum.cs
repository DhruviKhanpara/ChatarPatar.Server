using ChatarPatar.Common.AppExceptions.CustomExceptions;
using ChatarPatar.Common.Consts;
using Microsoft.AspNetCore.Http;

namespace ChatarPatar.Common.Enums;

public enum FileTypeEnum
{
    Image,
    Video,
    Audio,
    Document,
    Code,
    Archive,
    Other
}

public static class FileTypeExtensions
{
    /// <summary>
    /// Resolve folder name for cloudinary against the FileType.
    /// </summary
    public static FileFolderEnum ToMessageFolder(this FileTypeEnum fileType)
    {
        return fileType switch
        {
            FileTypeEnum.Image => FileFolderEnum.MessageImage,
            FileTypeEnum.Video => FileFolderEnum.MessageVideo,
            FileTypeEnum.Audio => FileFolderEnum.MessageAudio,
            _ => FileFolderEnum.MessageFile
        };
    }

    public static FileTypeEnum ValidateFile(this IFormFile file, FileUsageContextEnum context)
    {
        var fileType = file.GetFileType();
        file.ValidateMimeType(fileType);
        file.ValidateFileType(fileType, context);
        file.ValidateFileSize(fileType);
        return fileType;
    }

    #region Private section

    /// <summary>
    /// Resolve fileType against the file extensions.
    /// </summary
    private static FileTypeEnum GetFileType(this IFormFile file)
    {
        if (file == null || file.Length == 0)
            throw new AppException("Invalid file");

        var ext = Path.GetExtension(file.FileName).ToLower();

        return ext switch
        {
            ".jpg" or ".jpeg" or ".png" or ".gif" or ".webp" => FileTypeEnum.Image,
            ".mp4" or ".mov" or ".avi" or ".mkv" => FileTypeEnum.Video,
            ".mp3" or ".wav" or ".aac" => FileTypeEnum.Audio,
            ".pdf" or ".doc" or ".docx" or ".xls" or ".xlsx" or ".ppt" or ".pptx" or ".txt" => FileTypeEnum.Document,
            ".cs" or ".js" or ".ts" or ".html" or ".css" or ".json" or ".xml" => FileTypeEnum.Code,
            ".zip" or ".rar" or ".7z" or ".gz" => FileTypeEnum.Archive,
            _ => FileTypeEnum.Other
        };
    }

    /// <summary>
    /// Validates file size against the per-FileType size limits.
    /// </summary>
    private static void ValidateFileSize(this IFormFile file, FileTypeEnum fileType)
    {
        if (file == null || file.Length == 0)
            throw new AppException("Invalid file");

        long maxSize = fileType switch
        {
            FileTypeEnum.Image => FileSizeLimits.Image,
            FileTypeEnum.Video => FileSizeLimits.Video,
            FileTypeEnum.Audio => FileSizeLimits.Audio,
            FileTypeEnum.Document => FileSizeLimits.Document,
            FileTypeEnum.Code => FileSizeLimits.Code,
            FileTypeEnum.Archive => FileSizeLimits.Archive,
            _ => 5 * 1024 * 1024
        };

        if (file.Length > maxSize)
            throw new AppException($"File size exceeds allowed limit of {maxSize / (1024 * 1024)} MB.");
    }

    /// <summary>
    /// Validates that the file type is permitted for a given usage context.
    /// </summary>
    private static void ValidateFileType(this IFormFile file, FileTypeEnum fileType, FileUsageContextEnum context)
    {
        var allowed = context switch
        {
            FileUsageContextEnum.Avatar or
            FileUsageContextEnum.Org_Logo or
            FileUsageContextEnum.Team_Icon => new[] { FileTypeEnum.Image },

            FileUsageContextEnum.Attachment => new[]
            {
                FileTypeEnum.Image,
                FileTypeEnum.Video,
                FileTypeEnum.Audio,
                FileTypeEnum.Document,
                FileTypeEnum.Code,
                FileTypeEnum.Archive
            },

            _ => Array.Empty<FileTypeEnum>()
        };

        if (!allowed.Contains(fileType))
        {
            var allowedList = string.Join(", ", allowed);
            throw new InvalidDataAppException(
                $"File type '{fileType}' is not allowed for {context}. Allowed: {allowedList}.");
        }
    }

    /// <summary>
    /// Validates that the file mime type is permitted for a given file type (make much safer because extension alone can be spoofed).
    /// </summary>
    public static void ValidateMimeType(this IFormFile file, FileTypeEnum fileType)
    {
        if (!AllowedMimeTypes.MimeTypes.TryGetValue(fileType, out var allowed))
            return;

        if (!allowed.Contains(file.ContentType.ToLower()))
        {
            var allowedList = string.Join(", ", allowed);

            throw new InvalidDataAppException(
                $"Invalid MIME type '{file.ContentType}'. Allowed types: {allowedList}");
        }
    }

    #endregion
}