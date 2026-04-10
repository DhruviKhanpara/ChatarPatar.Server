using ChatarPatar.Infrastructure.Entities;
using ChatarPatar.Infrastructure.Persistence;
using ChatarPatar.Infrastructure.RepositoryContracts;

namespace ChatarPatar.Infrastructure.Repositories;

internal class OrganizationRepository : BaseSoftDeleteRepository<Organization>, IOrganizationRepository
{
    public OrganizationRepository(AppDbContext context) : base(context) { }

    public IQueryable<Organization> GetById(Guid id) => FindByCondition(x => x.Id == id).AsQueryable();

    public async Task<bool> SlugExistsAsync(string slug) => await AnyAsync(x => x.Slug == slug);
}

