using ChatarPatar.Application.DTOs.Organization;
using ChatarPatar.Common.Consts;
using FluentValidation;

namespace ChatarPatar.Application.Validators.Organization;

public class CreateOrganizationDtoValidator : AbstractValidator<CreateOrganizationDto>
{
    public CreateOrganizationDtoValidator()
    {
        var reserved = new[] { "admin", "api", "root", "system" };

        RuleFor(x => x.Name)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
                .WithMessage("Name is required.")
            .MaximumLength(ValidationConstants.Organization.Lengths.Name)
                .WithMessage($"Name must not exceed {ValidationConstants.Organization.Lengths.Name} characters.")
            .Matches(ValidationConstants.Organization.Patterns.Name)
                .WithMessage("Organization name can only contain letters, numbers, and spaces.");

        RuleFor(x => x.Slug)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
                .WithMessage("Slug is required.")
            .MaximumLength(ValidationConstants.Organization.Lengths.Slug)
                .WithMessage($"Name must not exceed {ValidationConstants.Organization.Lengths.Slug} characters.")
            .Matches(ValidationConstants.Organization.Patterns.Slug)
                .WithMessage("Slug can only contain lowercase letters, numbers, and hyphens. No leading/trailing or multiple hyphens.")
            .Must(slug => !reserved.Contains(slug))
                .WithMessage("This slug is reserved.");
    }
}
