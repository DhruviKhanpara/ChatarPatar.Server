using ChatarPatar.Infrastructure.Entities;

namespace ChatarPatar.Infrastructure.RepositoryContracts;

public interface IOrganizationRepository : IBaseSoftDeleteRepository<Organization>
{
    /// <summary>
    /// Returns a org by it's Id.
    /// </summary>
    IQueryable<Organization> GetById(Guid id);

    /// <summary>
    /// Check existence of slug in orgs.
    /// </summary>
    Task<bool> SlugExistsAsync(string slug, Guid? excludeTeamId = null);

    /// <summary>
    /// Returns a single org with its LogoFile navigation property loaded.
    /// </summary>
    IQueryable<Organization> GetByIdWithLogo(Guid id);
}

