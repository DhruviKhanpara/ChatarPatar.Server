using ChatarPatar.Application.DTOs.User;
using ChatarPatar.Common.Consts;
using FluentValidation;

namespace ChatarPatar.Application.Validators.User;

public class ForgotPasswordDtoValidator : AbstractValidator<ForgotPasswordDto>
{
    public ForgotPasswordDtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .MaximumLength(ValidationConstants.User.Lengths.Email).WithMessage($"Email must not exceed {ValidationConstants.User.Lengths.Email} characters.")
            .EmailAddress().WithMessage("Email is not a valid email address.");
    }
}