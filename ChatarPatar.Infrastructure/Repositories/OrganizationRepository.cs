using ChatarPatar.Infrastructure.Entities;
using ChatarPatar.Infrastructure.Persistence;
using ChatarPatar.Infrastructure.RepositoryContracts;
using Microsoft.EntityFrameworkCore;

namespace ChatarPatar.Infrastructure.Repositories;

internal class OrganizationRepository : BaseSoftDeleteRepository<Organization>, IOrganizationRepository
{
    public OrganizationRepository(AppDbContext context) : base(context) { }

    public IQueryable<Organization> GetById(Guid id) => 
        FindByCondition(x => x.Id == id);

    public async Task<bool> SlugExistsAsync(string slug) => 
        await AnyAsync(x => x.Slug == slug);

    public IQueryable<Organization> GetByIdWithLogo(Guid id) =>
        FindByCondition(x => x.Id == id)
            .Include(x => x.LogoFile);
}

