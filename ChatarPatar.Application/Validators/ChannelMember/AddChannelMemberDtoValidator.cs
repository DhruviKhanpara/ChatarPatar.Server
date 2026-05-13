using ChatarPatar.Application.DTOs.ChannelMember;
using FluentValidation;

namespace ChatarPatar.Application.Validators.ChannelMember;

public class AddChannelMemberDtoValidator : AbstractValidator<AddChannelMemberDto>
{
    public AddChannelMemberDtoValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
                .WithMessage("UserId is required.");

        RuleFor(x => x.Role)
            .IsInEnum()
                .WithMessage("Invalid channel role.");
    }
}