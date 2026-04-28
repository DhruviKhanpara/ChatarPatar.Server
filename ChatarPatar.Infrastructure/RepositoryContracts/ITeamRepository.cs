using ChatarPatar.Infrastructure.Entities;

namespace ChatarPatar.Infrastructure.RepositoryContracts;

public interface ITeamRepository : IBaseSoftDeleteRepository<Team>
{
    /// <summary>
    /// Returns a single active team by id, verifying it belongs to the given org.
    /// </summary>
    IQueryable<Team> GetByIdInOrg(Guid teamId, Guid orgId);

    /// <summary>
    /// Returns active (non-deleted) teams for an org.
    /// Caller controls whether archived teams are included.
    /// Optionally filters by name search term.
    /// </summary>
    IQueryable<Team> GetTeamsQuery(Guid orgId, Guid callerId, bool callerIsOrgAdmin, string? search = null, bool? isArchived = null, bool includePrivate = true);

    /// <summary>
    /// Returns true if a team with the given name already exists in the org (case-insensitive).
    /// </summary>
    Task<bool> NameExistsInOrgAsync(Guid orgId, string name, Guid? excludeTeamId = null);
}
