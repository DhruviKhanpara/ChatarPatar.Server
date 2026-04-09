using ChatarPatar.Application.DTOs.OrganizationMember;
using ChatarPatar.Common.Enums;
using FluentValidation;

namespace ChatarPatar.Application.Validators.OrganizationMember;

public class AddOrganizationMemberDtoValidator : AbstractValidator<AddOrganizationMemberDto>
{
    public AddOrganizationMemberDtoValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
                .WithMessage("User Id is required.")
            .NotEqual(Guid.Empty).WithMessage("A valid User Id must be provided.");

        RuleFor(x => x.Role)
            .IsInEnum()
                .WithMessage("The specified role is not a valid organization role.")
            .NotEqual(OrganizationRoleEnum.OrgOwner)
                .WithMessage("Cannot assign Owner role manually.");
    }
}
