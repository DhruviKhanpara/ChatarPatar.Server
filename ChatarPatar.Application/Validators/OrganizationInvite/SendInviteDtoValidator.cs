using ChatarPatar.Application.DTOs.OrganizationInvite;
using ChatarPatar.Common.Consts;
using ChatarPatar.Common.Enums;
using FluentValidation;

namespace ChatarPatar.Application.Validators.OrganizationInvite;

public class SendInviteDtoValidator : AbstractValidator<SendInviteDto>
{
    public SendInviteDtoValidator()
    {
        RuleFor(x => x.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
                .WithMessage("Email is required.")
            .EmailAddress()
                .WithMessage("Email is not valid.")
            .MaximumLength(ValidationConstants.Organization.Lengths.Email)
                .WithMessage($"Email must not exceed {ValidationConstants.Organization.Lengths.Email} characters.");

        RuleFor(x => x.Role)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
                .WithMessage("Role is required.")
            .Must(r => r is OrganizationRoleEnum.OrgAdmin
                          or OrganizationRoleEnum.OrgMember
                          or OrganizationRoleEnum.OrgGuest)
                .WithMessage("Role must be OrgAdmin, OrgMember, or OrgGuest. OrgOwner cannot be assigned via invite.");
    }
}
