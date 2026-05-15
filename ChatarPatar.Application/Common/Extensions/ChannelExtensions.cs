using ChatarPatar.Common.AppExceptions.CustomExceptions;
using ChatarPatar.Infrastructure.Entities;

namespace ChatarPatar.Application.Common.Extensions;

public static class ChannelExtensions
{
    public static void EnsureEditable(this Channel channel, string action = "not be modified")
    {
        if (channel.IsArchived)
        {
            throw new InvalidDataAppException($"Archived channel should {action}.");
        }

        channel.Team?.EnsureEditable("not allow modifications in their channels.");
    }
}
