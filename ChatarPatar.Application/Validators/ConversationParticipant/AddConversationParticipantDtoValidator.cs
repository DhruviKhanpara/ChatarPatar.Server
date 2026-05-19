using ChatarPatar.Application.DTOs.ConversationParticipant;
using FluentValidation;

namespace ChatarPatar.Application.Validators.ConversationParticipant;

public class AddConversationParticipantDtoValidator : AbstractValidator<AddConversationParticipantDto>
{
    public AddConversationParticipantDtoValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("UserId is required.");
    }
}
