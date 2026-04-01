using ChatarPatar.Application.DTOs.User;
using ChatarPatar.Common.Consts;
using FluentValidation;

namespace ChatarPatar.Application.Validators.User;

public class UserRegisterDtoValidator : AbstractValidator<UserRegisterDto>
{
    public UserRegisterDtoValidator()
    {
        RuleFor(x => x.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
                .WithMessage("Email is required.")
            .EmailAddress()
                .WithMessage("Email is not valid.")
            .MaximumLength(ValidationConstants.User.Lengths.Email)
                .WithMessage($"Email must not exceed {ValidationConstants.User.Lengths.Email} characters.");

        RuleFor(x => x.Username)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
                .WithMessage("Username is required.")
            .MinimumLength(ValidationConstants.User.Lengths.UsernameMin)
                .WithMessage($"Username must be at least {ValidationConstants.User.Lengths.UsernameMin} characters.")
            .MaximumLength(ValidationConstants.User.Lengths.Username)
                .WithMessage($"Username must not exceed {ValidationConstants.User.Lengths.Username} characters.")
            .Matches(ValidationConstants.User.Patterns.UserName)
                .WithMessage("Username can only contain lowercase letters, numbers, and underscores.");

        RuleFor(x => x.Name)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
                .WithMessage("Name is required.")
            .MaximumLength(ValidationConstants.User.Lengths.Name)
                .WithMessage($"Name must not exceed {ValidationConstants.User.Lengths.Name} characters.");

        RuleFor(x => x.Password)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Password is required.")
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

        // InviteToken rules — only checked when InviteToken is supplied
        When(x => !string.IsNullOrWhiteSpace(x.InviteToken), () =>
        {
            RuleFor(x => x.InviteToken!)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                    .WithMessage("Invite token cannot be empty.")
                .MaximumLength(ValidationConstants.Organization.Lengths.Token)
                    .WithMessage($"Invite token must not exceed {ValidationConstants.Organization.Lengths.Token} characters.");
        });
    }
}
