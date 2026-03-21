using ChatarPatar.Application.DTOs.User;
using ChatarPatar.Common.Consts;
using FluentValidation;

namespace ChatarPatar.Application.Validators.User;

internal class UserRegisterDtoValidator : AbstractValidator<UserRegisterDto>
{
    public UserRegisterDtoValidator()
    {
        RuleFor(x => x.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email is not valid.")
            .MaximumLength(FieldLengths.UserFields.Email).WithMessage($"Email must not exceed {FieldLengths.UserFields.Email} characters.");

        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Username is required.")
            .MaximumLength(FieldLengths.UserFields.Username).WithMessage($"Username must not exceed {FieldLengths.UserFields.Username} characters.")
            .Matches("^[a-zA-Z0-9_]+$").WithMessage("Username can only contain letters, numbers, and underscores.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(FieldLengths.UserFields.Name).WithMessage($"Name must not exceed {FieldLengths.UserFields.Name} characters.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(FieldLengths.UserFields.PasswordMin).WithMessage($"Password must be at least {FieldLengths.UserFields.PasswordMin} characters.")
            .MaximumLength(FieldLengths.UserFields.PasswordMax).WithMessage($"Password must not exceed {FieldLengths.UserFields.PasswordMax} characters.")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one number.");
    }
}
