using ChatarPatar.Common.AppExceptions.CustomExceptions;
using ChatarPatar.Common.Enums;
using ChatarPatar.Common.Models;
using ChatarPatar.Infrastructure.ExternalServiceContracts;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace ChatarPatar.Infrastructure.ExternalServices;

internal class CloudinaryService : ICloudinaryService
{
    private readonly Cloudinary _cloudinary;

    public CloudinaryService(IOptions<CloudinarySettings> settings)
    {
        var account = new Account(settings.Value.CloudName, settings.Value.ApiKey, settings.Value.ApiSecret);
        _cloudinary = new Cloudinary(account);
        _cloudinary.Api.Secure = true;
    }

    public async Task<FileUploadResult> UploadAttachmentAsync(IFormFile file, string folder, FileTypeEnum fileType)
    {
        if (file == null || file.Length == 0)
            throw new AppException("File is empty");

        await using var stream = file.OpenReadStream();

        var uploadParams = CreateUploadParams(file, stream, folder, fileType);

        var result = uploadParams switch
        {
            VideoUploadParams vid => await _cloudinary.UploadAsync(vid),
            ImageUploadParams img => await _cloudinary.UploadAsync(img),
            RawUploadParams raw => await _cloudinary.UploadAsync(raw),
            _ => throw new AppException("Unsupported upload type")
        };

        if (result.Error != null)
            throw new AppException($"Cloudinary error occurred: {result.Error.Message}");

        var thumbnailUrl = GenerateThumbnail(fileType, result.PublicId, file.FileName);

        return new FileUploadResult
        {
            PublicId = result.PublicId,
            Url = result.SecureUrl?.ToString() ?? string.Empty,
            ThumbnailUrl = thumbnailUrl
        };
    }

    public async Task<FileUploadResult> UploadProfileAssetAsync(IFormFile file, string folder, string publicId)
    {
        if (file == null || file.Length == 0)
            throw new AppException("File is empty");

        await using var stream = file.OpenReadStream();

        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(file.FileName, stream),
            Folder = folder,
            PublicId = publicId,
            Overwrite = true
        };

        var result = await _cloudinary.UploadAsync(uploadParams);

        if (result.Error != null)
            throw new AppException($"Cloudinary error occurred: {result.Error.Message}");

        var thumbnailUrl = GenerateImageThumbnail(result.PublicId);

        return new FileUploadResult
        {
            PublicId = result.PublicId,
            Url = result.SecureUrl?.ToString() ?? string.Empty,
            ThumbnailUrl = thumbnailUrl
        };
    }

    public async Task<bool> DeleteFileAsync(string publicId)
    {
        var deletionParams = new DeletionParams(publicId);
        var result = await _cloudinary.DestroyAsync(deletionParams);

        if (result.Error != null)
        {
            throw new AppException($"Cloudinary error occurred: {result.Error.Message}");
        }

        return result.Result == "ok";
    }

    public async Task<int> DeleteByPrefixAsync(string prefix)
    {
        string nextCursor = null;
        int deleted = 0;

        do
        {
            var deleteParams = new DelResParams
            {
                Prefix = prefix,
                ResourceType = ResourceType.Auto,
                All = true,
                NextCursor = nextCursor
            };

            var result = await _cloudinary.DeleteResourcesAsync(deleteParams);

            if (result.Error != null)
            {
                throw new AppException($"Cloudinary error: {result.Error.Message}");
            }

            deleted += result.Deleted.Count;
            nextCursor = result.NextCursor;

        } while (!string.IsNullOrEmpty(nextCursor));

        return deleted;
    }

    #region Private Section

    #region Upload Helpers

    private object CreateUploadParams(IFormFile file, Stream stream, string folder, FileTypeEnum fileType)
    {
        var fileDescription = new FileDescription(file.FileName, stream);

        return fileType switch
        {
            FileTypeEnum.Image => new ImageUploadParams
            {
                File = fileDescription,
                Folder = folder,
                UseFilename = true,
                UniqueFilename = true,
                Overwrite = false
            },

            FileTypeEnum.Video => new VideoUploadParams
            {
                File = fileDescription,
                Folder = folder,
                UseFilename = true,
                UniqueFilename = true,
                Overwrite = false
            },

            _ => new RawUploadParams
            {
                File = fileDescription,
                Folder = folder,
                UseFilename = true,
                UniqueFilename = true,
                Overwrite = false
            }
        };
    }

    private string? GenerateThumbnail(FileTypeEnum fileType, string publicId, string fileName)
    {
        if (fileType == FileTypeEnum.Image)
            return GenerateImageThumbnail(publicId);

        if (fileType == FileTypeEnum.Video)
            return GenerateVideoThumbnail(publicId);

        if (Path.GetExtension(fileName).ToLower() == ".pdf")
            return GeneratePdfThumbnail(publicId);

        return null;
    }

    #endregion

    #region Thumbnail Generators

    private string GenerateImageThumbnail(string publicId)
    {
        return _cloudinary.Api.UrlImgUp
            .Transform(new Transformation()
                .Width(100)
                .Height(100)
                .Crop("thumb")
                .Gravity("face")
                .Radius("max"))
            .BuildUrl(publicId);
    }

    private string GeneratePdfThumbnail(string publicId)
    {
        return _cloudinary.Api.UrlImgUp
            .Transform(new Transformation()
                .Width(200)
                .Page(1))
            .BuildUrl(publicId + ".pdf");
    }

    private string GenerateVideoThumbnail(string publicId)
    {
        return _cloudinary.Api.UrlImgUp
            .ResourceType("video")
            .Transform(new Transformation()
                .Width(200)
                .Height(200)
                .Crop("fill")
                .StartOffset("1"))
            .BuildUrl(publicId + ".mp4");
    }

    #endregion

    #endregion
}
