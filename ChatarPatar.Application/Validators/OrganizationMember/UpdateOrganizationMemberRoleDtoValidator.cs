using ChatarPatar.Application.DTOs.OrganizationMember;
using ChatarPatar.Common.Enums;
using FluentValidation;

namespace ChatarPatar.Application.Validators.OrganizationMember;

public class UpdateOrganizationMemberRoleDtoValidator : AbstractValidator<UpdateOrganizationMemberRoleDto>
{
    public UpdateOrganizationMemberRoleDtoValidator()
    {
        RuleFor(x => x.Role)
            .IsInEnum()
                .WithMessage("The specified role is not a valid organization role.")
            .NotEqual(OrganizationRoleEnum.OrgOwner)
                .WithMessage("Cannot assign Owner role manually.");
    }
}
