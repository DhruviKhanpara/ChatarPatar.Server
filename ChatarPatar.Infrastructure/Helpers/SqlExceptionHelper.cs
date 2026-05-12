using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace ChatarPatar.Infrastructure.Helpers;

public static class SqlExceptionHelper
{
    public static bool IsUniqueConstraintViolation(this DbUpdateException exception, out string message)
    {
        message = null!;

        if (exception.InnerException is not SqlException sqlEx || sqlEx.Number is not (2601 or 2627))
            return false;

        var constraintName = ExtractConstraintName(sqlEx.Message);

        message = ConstraintMessages.GetValueOrDefault(constraintName ?? string.Empty, "Duplicate value already exists.");

        return true;
    }

    private static string? ExtractConstraintName(string message)
    {
        var match = Regex.Match(message, @"(?:constraint|index) '([^']+)'", RegexOptions.IgnoreCase);

        return match.Success
            ? match.Groups[1].Value
            : null;
    }

    private static readonly Dictionary<string, string> ConstraintMessages = new(StringComparer.OrdinalIgnoreCase)
    {
        ["UQ_Users_Email"] = "Email already exists.",
        ["UQ_Users_Username"] = "Username already exists.",
        ["UQ_Organizations_Slug"] = "Organization slug already exists.",
        ["UX_OrgMembers_Active"] = "User is already a member of the organization.",
        ["UX_Teams_Name"] = "Team name already exists in Organization.",
        ["UX_TeamMembers_Active"] = "User is already a member of the team.",
        ["UX_Channels_Name"] = "Channel name already exists in this team.",
        ["UX_ChannelMembers_Active"] = "User is already a member of the channel.",
        ["UX_ConvParticipants_Active"] = "User is already a participant in the conversation.",
    };
}
