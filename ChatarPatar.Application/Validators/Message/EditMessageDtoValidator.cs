using ChatarPatar.Application.DTOs.Message;
using ChatarPatar.Common.Consts;
using FluentValidation;

namespace ChatarPatar.Application.Validators.Message;

public class EditMessageDtoValidator : AbstractValidator<EditMessageDto>
{
    public EditMessageDtoValidator()
    {
        // If content is provided it must not be blank
        When(x => x.Content != null, () =>
        {
            RuleFor(x => x.Content)
                .NotEmpty()
                    .WithMessage("Content cannot be empty or whitespace only.")
                .MaximumLength(ValidationConstants.Message.Lengths.Content)
                    .WithMessage($"Content cannot exceed {ValidationConstants.Message.Lengths.Content} characters.");
        });

        // After the edit the message must have content OR at least one file
        RuleFor(x => x.Content)
            .Must((dto, content) => !string.IsNullOrWhiteSpace(content) || dto.FileIds.Count > 0)
                .WithMessage("A message must have content or at least one attachment after editing.");

        // Attachment count cap (same as send)
        RuleFor(x => x.FileIds)
            .Cascade(CascadeMode.Stop)
            .Must(ids => ids.Count <= ValidationConstants.Message.Lengths.MaxAttachmentsPerMessage)
                .WithMessage($"A message can have at most {ValidationConstants.Message.Lengths.MaxAttachmentsPerMessage} attachments.")
            .Must(ids => ids.Distinct().Count() == ids.Count)
                .WithMessage("FileIds must not contain duplicates.");

        // No duplicate mentions
        RuleFor(x => x.MentionedUserIds)
            .Must(ids => ids.Distinct().Count() == ids.Count)
                .WithMessage("MentionedUserIds must not contain duplicates.");
    }
}
