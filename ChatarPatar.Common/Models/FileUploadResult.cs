namespace ChatarPatar.Common.Models;

public class FileUploadResult
{
    public string PublicId { get; set; } = null!;
    public string Url { get; set; } = null!;
    public string? ThumbnailUrl { get; set; }
}
