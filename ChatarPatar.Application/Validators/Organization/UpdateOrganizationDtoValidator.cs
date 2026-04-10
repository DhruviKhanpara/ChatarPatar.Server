using ChatarPatar.Application.DTOs.Organization;
using ChatarPatar.Common.Consts;
using FluentValidation;

namespace ChatarPatar.Application.Validators.Organization;

public class UpdateOrganizationDtoValidator : AbstractValidator<UpdateOrganizationDto>
{
    public UpdateOrganizationDtoValidator()
    {
        RuleFor(x => x.Name)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
                .WithMessage("Name is required.")
            .MaximumLength(ValidationConstants.Organization.Lengths.Name)
                .WithMessage($"Name must not exceed {ValidationConstants.Organization.Lengths.Name} characters.")
            .Matches(ValidationConstants.Organization.Patterns.Name)
                .WithMessage("Organization name can only contain letters, numbers, and spaces.");
    }
}
