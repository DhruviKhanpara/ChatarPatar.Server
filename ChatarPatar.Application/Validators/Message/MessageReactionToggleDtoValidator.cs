using ChatarPatar.Application.DTOs.Message.Reaction;
using ChatarPatar.Common.Consts;
using FluentValidation;
using System.Text.RegularExpressions;

namespace ChatarPatar.Application.Validators.Message;

public class MessageReactionToggleDtoValidator : AbstractValidator<MessageReactionToggleDto>
{
    public MessageReactionToggleDtoValidator()
    {
        RuleFor(x => x.Emoji)
            .NotEmpty()
                .WithMessage("Emoji is required.")
            .MaximumLength(ValidationConstants.Message.Lengths.Emoji)
                .WithMessage($"Emoji must be at most {ValidationConstants.Message.Lengths.Emoji} characters.")
            .Must(x => x == x.Trim())
                .WithMessage("Emoji cannot contain leading or trailing whitespace.")
            .Must(emoji => !EmojiShortcodeRegex.IsMatch(emoji))
                .WithMessage("Emoji must be a Unicode character (e.g. 👍), not a shortcode (e.g. :thumbsup:).");
    }

    private static readonly Regex EmojiShortcodeRegex = new(ValidationConstants.Message.Patterns.EmojiShortcode, RegexOptions.Compiled | RegexOptions.IgnoreCase);

}
