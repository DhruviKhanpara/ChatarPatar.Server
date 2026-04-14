using ChatarPatar.Application.DTOs.User;
using ChatarPatar.Common.Consts;
using FluentValidation;

namespace ChatarPatar.Application.Validators.User;

public class VerifyEmailDtoValidator : AbstractValidator<VerifyEmailDto>
{
    public VerifyEmailDtoValidator()
    {
        RuleFor(x => x.Otp)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
                .WithMessage("OTP is required.")
            .Matches(ValidationConstants.OtpVerification.Patterns.Otp)
                .WithMessage("OTP must be a 6-digit number.");
    }
}
