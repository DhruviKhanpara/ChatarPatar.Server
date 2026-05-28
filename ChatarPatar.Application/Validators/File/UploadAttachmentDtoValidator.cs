using ChatarPatar.Application.DTOs.File;
using ChatarPatar.Common.Consts;
using FluentValidation;

namespace ChatarPatar.Application.Validators.File;

public class UploadAttachmentDtoValidator : AbstractValidator<UploadAttachmentDto>
{
    public UploadAttachmentDtoValidator()
    {
        // File itself
        RuleFor(x => x.File)
            .Cascade(CascadeMode.Stop)
            .NotNull()
                .WithMessage("File is required.")
            .Must(f => f.Length > 0)
                .WithMessage("File cannot be empty.")
            .Must(f => f.Length <= MaxSize)
                .WithMessage($"File exceeds the maximum allowed size of {MaxSize / (1024 * 1024)} MB.")
            .Must(f => AllowedMimes.Contains(f.ContentType))
                .WithMessage("File type is not allowed.");

        // Exactly one scope
        RuleFor(x => x)
            .Must(x => (x.ChannelId.HasValue) != (x.ConversationId.HasValue))
                .WithMessage("Exactly one of ChannelId or ConversationId must be provided.");

        // Channel upload requires OrgId + TeamId
        When(x => x.ChannelId.HasValue, () =>
        {
            RuleFor(x => x.OrgId)
                .NotEmpty()
                    .WithMessage("OrgId is required for channel attachments.");

            RuleFor(x => x.TeamId)
                .NotEmpty()
                    .WithMessage("TeamId is required for channel attachments.");
        });
    }

    private static readonly HashSet<string> AllowedMimes =
        AllowedMimeTypes.MimeTypes
            .SelectMany(kv => kv.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

    private static readonly long MaxSize = FileSizeLimits.Video;
}
