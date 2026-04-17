using ChatarPatar.Application.DTOs.TeamMember;
using FluentValidation;

namespace ChatarPatar.Application.Validators.TeamMember;

public class UpdateTeamMemberRoleDtoValidator : AbstractValidator<UpdateTeamMemberRoleDto>
{
    public UpdateTeamMemberRoleDtoValidator()
    {
        RuleFor(x => x.Role)
            .IsInEnum()
                .WithMessage("The specified role is not a valid team role.");
    }
}
