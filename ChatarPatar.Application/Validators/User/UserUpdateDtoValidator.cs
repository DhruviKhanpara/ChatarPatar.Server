using ChatarPatar.Application.DTOs.User;
using ChatarPatar.Common.Consts;
using FluentValidation;

namespace ChatarPatar.Application.Validators.User;

public class UserUpdateDtoValidator : AbstractValidator<UserUpdateDto>
{
    public UserUpdateDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
                .WithMessage("Name is required.")
            .MaximumLength(ValidationConstants.User.Lengths.Name)
                .WithMessage($"Name must not exceed {ValidationConstants.User.Lengths.Name} characters.");

        When(x => !string.IsNullOrWhiteSpace(x.Bio), () =>
        {
            RuleFor(x => x.Bio!)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                    .WithMessage("Bio is required.")
                .MaximumLength(ValidationConstants.User.Lengths.Bio)
                    .WithMessage($"Bio must not exceed {ValidationConstants.User.Lengths.Bio} characters.");
        });     
    }
}
