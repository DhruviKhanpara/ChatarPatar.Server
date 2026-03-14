using ChatarPatar.Infrastructure.Entities;
using ChatarPatar.Infrastructure.Persistence;
using ChatarPatar.Infrastructure.RepositoryContracts;

namespace ChatarPatar.Infrastructure.Repositories;

internal class OrganizationMemberRepository : BaseSoftDeleteRepository<OrganizationMember>, IOrganizationMemberRepository
{
    public OrganizationMemberRepository(AppDbContext context) : base(context) { }

    public IQueryable<OrganizationMember> GetOrgMemberAsync(Guid userId, Guid orgId) => FindByCondition(x => x.UserId == userId && x.OrgId == orgId).AsQueryable();
}