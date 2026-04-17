using ChatarPatar.Infrastructure.Entities;

namespace ChatarPatar.Infrastructure.RepositoryContracts;

public interface ITeamRepository : IBaseSoftDeleteRepository<Team>
{
    /// <summary>
    /// Returns a single active team by id, verifying it belongs to the given org.
    /// </summary>
    IQueryable<Team> GetByIdInOrg(Guid teamId, Guid orgId);

    /// <summary>
    /// Returns true if a team with the given name already exists in the org (case-insensitive).
    /// </summary>
    Task<bool> NameExistsInOrgAsync(Guid orgId, string name, Guid? excludeTeamId = null);
}
