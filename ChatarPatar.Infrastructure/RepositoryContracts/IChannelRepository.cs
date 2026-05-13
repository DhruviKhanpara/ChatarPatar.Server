using ChatarPatar.Infrastructure.Entities;

namespace ChatarPatar.Infrastructure.RepositoryContracts;

public interface IChannelRepository : IBaseSoftDeleteRepository<Channel>
{
    /// <summary>
    /// Returns a single active channel by id, verifying it belongs to the given team and org.
    /// </summary>
    IQueryable<Channel> GetByIdInTeam(Guid channelId, Guid teamId, Guid orgId);

    /// <summary>
    /// Returns a filterable, orderable query of active channels within a team.
    /// Caller controls visibility of private channels.
    /// </summary>
    IQueryable<Channel> GetChannelsQuery(Guid teamId, Guid orgId, Guid callerId, bool callerHasElevatedAccess, string? search = null, bool? isArchived = null, bool includePrivate = true);

    /// <summary>
    /// Returns true if a channel with the given name already exists in the team (case-insensitive).
    /// </summary>
    Task<bool> NameExistsInTeamAsync(Guid teamId, string name, Guid? excludeChannelId = null);
}

