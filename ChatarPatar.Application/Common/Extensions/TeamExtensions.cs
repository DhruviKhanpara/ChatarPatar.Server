using ChatarPatar.Common.AppExceptions.CustomExceptions;
using ChatarPatar.Infrastructure.Entities;

namespace ChatarPatar.Application.Common.Extensions;

public static class TeamExtensions
{
    public static void EnsureEditable(this Team team, string action = "modified")
    {
        if (team.IsArchived)
        {
            throw new InvalidDataAppException($"Archived teams cannot be {action}.");
        }
    }
}
