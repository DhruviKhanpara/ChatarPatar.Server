using ChatarPatar.Application.DTOs.Message;
using ChatarPatar.Common.Consts;
using FluentValidation;

namespace ChatarPatar.Application.Validators.Message;

public class SendMessageDtoValidator : AbstractValidator<SendMessageDto>
{
    public SendMessageDtoValidator()
    {
        RuleFor(x => x.ClientMessageId)
            .NotEmpty()
                .WithMessage("ClientMessageId is required.");

        // Content length
        When(x => x.Content != null, () =>
        {
            RuleFor(x => x.Content)
                .NotEmpty()
                    .WithMessage("Content cannot be empty or whitespace only.")
                .MaximumLength(ValidationConstants.Message.Lengths.Content)
                    .WithMessage($"Content cannot exceed {ValidationConstants.Message.Lengths.Content} characters.");
        });

        // Content OR attachments required
        RuleFor(x => x.Content)
            .Must((dto, content) => !string.IsNullOrWhiteSpace(content) || dto.FileIds.Count > 0)
                .WithMessage("A message must have content or at least one attachment.");

        // Attachment count cap
        RuleFor(x => x.FileIds)
            .Cascade(CascadeMode.Stop)
            .Must(ids => ids.Count <= ValidationConstants.Message.Lengths.MaxAttachmentsPerMessage)
                .WithMessage($"A message can have at most {ValidationConstants.Message.Lengths.MaxAttachmentsPerMessage} attachments.")
            .Must(ids => ids.Distinct().Count() == ids.Count)    // No duplicate FileIds
                .WithMessage("FileIds must not contain duplicates.");

        // No duplicate MentionedUserIds
        RuleFor(x => x.MentionedUserIds)
            .Must(ids => ids.Distinct().Count() == ids.Count)
                .WithMessage("MentionedUserIds must not contain duplicates.");
    }
}
