using ChatarPatar.Infrastructure.Entities;

namespace ChatarPatar.Infrastructure.RepositoryContracts;

public interface IOrganizationMemberRepository : IBaseSoftDeleteRepository<OrganizationMember>
{
    IQueryable<OrganizationMember> GetById(Guid id);
    IQueryable<OrganizationMember> GetOrgMemberAsync(Guid userId, Guid orgId);
}