using ChatarPatar.Infrastructure.Entities;

namespace ChatarPatar.Infrastructure.RepositoryContracts;

public interface IOrganizationRepository : IBaseSoftDeleteRepository<Organization>
{
    IQueryable<Organization> GetById(Guid id);
    Task<bool> SlugExistsAsync(string slug);
}

