using ChatarPatar.Common.AppExceptions.CustomExceptions;
using ChatarPatar.Infrastructure.Entities;

namespace ChatarPatar.Application.Common.Extensions;

public static class ChannelExtensions
{
    public static void EnsureEditable(this Channel channel, string action = "modified")
    {
        if (channel.IsArchived)
        {
            throw new InvalidDataAppException($"Archived channel cannot be {action}.");
        }

        channel.Team?.EnsureEditable(action);
    }
}
