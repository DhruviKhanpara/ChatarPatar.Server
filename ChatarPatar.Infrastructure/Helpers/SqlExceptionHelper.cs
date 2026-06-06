using ChatarPatar.Common.Consts;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace ChatarPatar.Infrastructure.Helpers;

public static class SqlExceptionHelper
{
    public static bool IsDirectConversationUniqueViolation(this DbUpdateException ex)
    {
        if (ex.InnerException is SqlException sqlEx)
        {
            // 2601 = duplicate key
            // 2627 = unique constraint violation
            return sqlEx.Number is 2601 or 2627
                && sqlEx.Message.Contains(DbConstraints.Conversations.UniqueDirectConversationParticipants);
        }

        return false;
    }

    public static bool IsPinnedMessagePerChannelUniqueViolation(this DbUpdateException ex)
    {
        if (ex.InnerException is SqlException sqlEx)
        {
            return sqlEx.Number is 2601 or 2627
                && sqlEx.Message.Contains(DbConstraints.PinnedMessages.UniquePinnedMessagePerChannel);
        }

        return false;
    }

    public static bool IsPinnedMessagePerConversationUniqueViolation(this DbUpdateException ex)
    {
        if (ex.InnerException is SqlException sqlEx)
        {
            return sqlEx.Number is 2601 or 2627
                && sqlEx.Message.Contains(DbConstraints.PinnedMessages.UniquePinnedMessagePerConversation);
        }

        return false;
    }

    public static bool IsUniqueConstraintViolation(this DbUpdateException exception, out string message)
    {
        message = null!;

        if (exception.InnerException is not SqlException sqlEx || sqlEx.Number is not (2601 or 2627))
            return false;

        var constraintName = ExtractConstraintName(sqlEx.Message);

        message = ConstraintMessages.GetValueOrDefault(constraintName ?? string.Empty, "Duplicate value already exists.");

        return true;
    }

    #region Private Section

    private static string? ExtractConstraintName(string message)
    {
        var match = Regex.Match(message, @"(?:constraint|index) '([^']+)'", RegexOptions.IgnoreCase);

        return match.Success
            ? match.Groups[1].Value
            : null;
    }

    private static readonly Dictionary<string, string> ConstraintMessages = new(StringComparer.OrdinalIgnoreCase)
    {
        [DbConstraints.Users.UniqueEmail] = "Email already exists.",
        [DbConstraints.Users.UniqueUsername] = "Username already exists.",
        [DbConstraints.Organizations.UniqueSlug] = "Organization slug already exists.",
        [DbConstraints.OrganizationMembers.UniqueActiveOrgMembers] = "User is already a member of the organization.",
        [DbConstraints.Teams.UniqueNamePerOrg] = "Team name already exists in Organization.",
        [DbConstraints.TeamMembers.UniqueActiveTeamMembers] = "User is already a member of the team.",
        [DbConstraints.Channels.UniqueNamePerTeam] = "Channel name already exists in this team.",
        [DbConstraints.ChannelMembers.UniqueActiveChannelMembers] = "User is already a member of the channel.",
        [DbConstraints.ConversationParticipants.UniqueConversationUser] = "User is already a participant in the conversation.",
    };

    #endregion
}
