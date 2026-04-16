using ChatarPatar.Application.DTOs.User;
using ChatarPatar.Common.Consts;
using FluentValidation;

namespace ChatarPatar.Application.Validators.User;

public class ChangePasswordDtoValidator : AbstractValidator<ChangePasswordDto>
{
    public ChangePasswordDtoValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty()
                .WithMessage("Current Password is required.")
            .MaximumLength(ValidationConstants.User.Lengths.PasswordMax);

        RuleFor(x => x.NewPassword)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("New password is required.")
            .MinimumLength(ValidationConstants.User.Lengths.PasswordMin)
                .WithMessage($"Password must be at least {ValidationConstants.User.Lengths.PasswordMin} characters.")
            .MaximumLength(ValidationConstants.User.Lengths.PasswordMax)
                .WithMessage($"Password must not exceed {ValidationConstants.User.Lengths.PasswordMax} characters.")
            .Matches(ValidationConstants.User.Patterns.HasUppercase)
                .WithMessage("Password must contain at least one uppercase letter.")
            .Matches(ValidationConstants.User.Patterns.HasLowercase)
                .WithMessage("Password must contain at least one lowercase letter.")
            .Matches(ValidationConstants.User.Patterns.HasNumber)
                .WithMessage("Password must contain at least one number.")
            .Matches(ValidationConstants.User.Patterns.HasSpecialChar)
                .WithMessage("Password must contain at least one special character (@$!%*?&).");
    }
}
