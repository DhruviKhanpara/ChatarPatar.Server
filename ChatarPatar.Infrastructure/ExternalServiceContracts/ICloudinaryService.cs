using ChatarPatar.Common.Enums;
using ChatarPatar.Common.Models;
using Microsoft.AspNetCore.Http;

namespace ChatarPatar.Infrastructure.ExternalServiceContracts;

public interface ICloudinaryService
{
    Task<FileUploadResult> UploadAttachmentAsync(IFormFile file, string folder, FileTypeEnum fileType); 
    Task<FileUploadResult> UploadProfileAssetAsync(IFormFile file, string folder, string publicId); 
    Task<bool> DeleteFileAsync(string publicId);
    Task<int> DeleteByPrefixAsync(string prefix);
}
