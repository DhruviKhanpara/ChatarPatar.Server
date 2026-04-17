using ChatarPatar.Application.DTOs.Team;
using ChatarPatar.Common.Consts;
using FluentValidation;

namespace ChatarPatar.Application.Validators.Team;

public class CreateTeamDtoValidator : AbstractValidator<CreateTeamDto>
{
    public CreateTeamDtoValidator()
    {
        RuleFor(x => x.Name)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
                .WithMessage("Team name is required.")
            .MaximumLength(ValidationConstants.Team.Lengths.Name)
                .WithMessage($"Team name must not exceed {ValidationConstants.Team.Lengths.Name} characters.");

        When(x => !string.IsNullOrWhiteSpace(x.Description), () =>
        {
            RuleFor(x => x.Description)
                .MaximumLength(ValidationConstants.Team.Lengths.Description)
                    .WithMessage($"Description must not exceed {ValidationConstants.Team.Lengths.Description} characters.");
        });
    }
}
