using ChatarPatar.Application.DTOs.ConversationParticipant;
using FluentValidation;

namespace ChatarPatar.Application.Validators.ConversationParticipant;

public class UpdateConversationParticipantRoleDtoValidator : AbstractValidator<UpdateConversationParticipantRoleDto>
{
    public UpdateConversationParticipantRoleDtoValidator()
    {
        RuleFor(x => x.Role)
            .IsInEnum()
                .WithMessage("Invalid participant role.");
    }
}
