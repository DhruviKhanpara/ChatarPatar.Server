using ChatarPatar.Application.DTOs.User;
using ChatarPatar.Common.Consts;
using ChatarPatar.Common.Enums;
using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace ChatarPatar.Application.Validators.User;

public class UpdateAvatarDtoValidator : AbstractValidator<UpdateAvatarDto>
{
    public UpdateAvatarDtoValidator()
    {
        RuleFor(x => x.AvatarFile)
            .Cascade(CascadeMode.Stop)
            .NotNull()
                .WithMessage("Profile photo is required")
            .Must(file => file.Length > 0)
                .WithMessage("File cannot be empty")
            .Must(file => file.Length <= FileSizeLimits.Image)
                .WithMessage($"File size exceeds allowed limit of {FileSizeLimits.Image / (1024 * 1024)} MB.")
            .Must(BeValidImageType)
                .WithMessage("Only JPEG, PNG, and WEBP images are allowed");
    }

    private bool BeValidImageType(IFormFile file)
    {
        if (!AllowedMimeTypes.MimeTypes.TryGetValue(FileTypeEnum.Image, out var allowed))
            return false;
        
        return allowed.Contains(file.ContentType);
    }
}
