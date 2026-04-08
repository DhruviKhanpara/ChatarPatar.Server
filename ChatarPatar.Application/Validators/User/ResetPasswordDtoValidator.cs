using ChatarPatar.Application.DTOs.User;
using ChatarPatar.Common.Consts;
using FluentValidation;

namespace ChatarPatar.Application.Validators.User;

public class ResetPasswordDtoValidator : AbstractValidator<ResetPasswordDto>
{
    public ResetPasswordDtoValidator()
    {
        RuleFor(x => x.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
                .WithMessage("Email is required.")
            .EmailAddress()
                .WithMessage("Email is not valid.")
            .MaximumLength(ValidationConstants.User.Lengths.Email)
                .WithMessage($"Email must not exceed {ValidationConstants.User.Lengths.Email} characters.");

        RuleFor(x => x.Otp)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
                .WithMessage("OTP is required.")
            .Length(6)
                .WithMessage("OTP must be exactly 6 digits.")
            .Matches(ValidationConstants.OtpVerification.Patterns.Otp)
                .WithMessage("OTP must contain only digits.");

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
