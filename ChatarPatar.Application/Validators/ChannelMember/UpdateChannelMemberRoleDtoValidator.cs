using ChatarPatar.Application.DTOs.ChannelMember;
using FluentValidation;

namespace ChatarPatar.Application.Validators.ChannelMember;

public class UpdateChannelMemberRoleDtoValidator : AbstractValidator<UpdateChannelMemberRoleDto>
{
    public UpdateChannelMemberRoleDtoValidator()
    {
        RuleFor(x => x.Role)
            .IsInEnum()
                .WithMessage("Invalid channel role.");
    }
}
