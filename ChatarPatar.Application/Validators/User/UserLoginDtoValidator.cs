using ChatarPatar.Application.DTOs.User;
using ChatarPatar.Common.Consts;
using FluentValidation;

namespace ChatarPatar.Application.Validators.User;

public class UserLoginDtoValidator : AbstractValidator<UserLoginDto>
{
    public UserLoginDtoValidator()
    {
        RuleFor(x => x.Identifier)
            .NotEmpty().WithMessage("Username or email is required.")
            .MaximumLength(Math.Max(ValidationConstants.User.Lengths.Email, ValidationConstants.User.Lengths.Username));

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MaximumLength(ValidationConstants.User.Lengths.PasswordMax);
    }
}
