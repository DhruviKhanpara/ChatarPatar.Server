using ChatarPatar.Application.DTOs.TeamMember;
using ChatarPatar.Common.Enums;
using FluentValidation;

namespace ChatarPatar.Application.Validators.TeamMember;

public class AddTeamMemberDtoValidator : AbstractValidator<AddTeamMemberDto>
{
    public AddTeamMemberDtoValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
                .WithMessage("User Id is required.")
            .NotEqual(Guid.Empty).WithMessage("A valid User Id must be provided.");

        RuleFor(x => x.Role)
            .IsInEnum()
                .WithMessage("The specified role is not a valid team role.");
    }
}
