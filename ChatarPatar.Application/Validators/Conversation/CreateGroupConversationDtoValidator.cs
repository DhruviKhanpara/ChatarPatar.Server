using ChatarPatar.Application.DTOs.Conversation;
using ChatarPatar.Common.Consts;
using FluentValidation;

namespace ChatarPatar.Application.Validators.Conversation;

public class CreateGroupConversationDtoValidator : AbstractValidator<CreateGroupConversationDto>
{
    public CreateGroupConversationDtoValidator()
    {
        RuleFor(x => x.Name)
            .Must(name => !string.IsNullOrWhiteSpace(name))
                .WithMessage("Group name is required.")
            .MaximumLength(ValidationConstants.Conversation.Lengths.Name)
                .WithMessage($"Group name must not exceed {ValidationConstants.Conversation.Lengths.Name} characters.");

        RuleFor(x => x.ParticipantUserIds)
            .Cascade(CascadeMode.Stop)
            .NotNull()
                .WithMessage("Participant users are required.")
            .Must(ids => ids.Count >= 2)
                .WithMessage("A group conversation requires at least 2 other participants.")
            .Must(ids => ids.Distinct().Count() == ids.Count)
                .WithMessage("Duplicate participant users are not allowed.");
    }
}
