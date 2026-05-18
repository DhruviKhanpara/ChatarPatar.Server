using ChatarPatar.Application.DTOs.Conversation;
using FluentValidation;

namespace ChatarPatar.Application.Validators.Conversation;

public class CreateDirectConversationDtoValidator : AbstractValidator<CreateDirectConversationDto>
{
    public CreateDirectConversationDtoValidator()
    {
        RuleFor(x => x.TargetUserId)
            .NotEmpty()
            .WithMessage("TargetUserId is required.");
    }
}
