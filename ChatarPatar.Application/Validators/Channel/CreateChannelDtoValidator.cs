using ChatarPatar.Application.DTOs.Channel;
using ChatarPatar.Common.Consts;
using FluentValidation;

namespace ChatarPatar.Application.Validators.Channel;

public class CreateChannelDtoValidator : AbstractValidator<CreateChannelDto>
{
    public CreateChannelDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
                .WithMessage("Channel name is required.")
            .MaximumLength(ValidationConstants.Channel.Lengths.Name)
                .WithMessage("Channel name must not exceed 100 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(ValidationConstants.Channel.Lengths.Description)
                .WithMessage("Description must not exceed 500 characters.")
            .When(x => x.Description != null);

        RuleFor(x => x.Type)
            .IsInEnum()
                .WithMessage("Invalid channel type.");
    }
}
