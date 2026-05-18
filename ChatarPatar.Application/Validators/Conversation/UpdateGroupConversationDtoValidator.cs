using ChatarPatar.Application.DTOs.Conversation;
using ChatarPatar.Common.Consts;
using FluentValidation;

namespace ChatarPatar.Application.Validators.Conversation;

public class UpdateGroupConversationDtoValidator : AbstractValidator<UpdateGroupConversationDto>
{
    public UpdateGroupConversationDtoValidator()
    {
        RuleFor(x => x.Name)
            .Must(name => !string.IsNullOrWhiteSpace(name))
                .WithMessage("Group name is required.")
            .MaximumLength(ValidationConstants.Conversation.Lengths.Name)
                .WithMessage($"Group name must not exceed {ValidationConstants.Conversation.Lengths.Name} characters.");
    }
}
