using ChatarPatar.Common.Enums;
using ChatarPatar.Infrastructure.Entities;
using ChatarPatar.Infrastructure.Persistence;
using ChatarPatar.Infrastructure.RepositoryContracts;
using Microsoft.EntityFrameworkCore;

namespace ChatarPatar.Infrastructure.Repositories;

internal class OrganizationMemberRepository : BaseSoftDeleteRepository<OrganizationMember>, IOrganizationMemberRepository
{
    public OrganizationMemberRepository(AppDbContext context) : base(context) { }

    public IQueryable<OrganizationMember> GetById(Guid id) =>
        FindByCondition(x => x.Id == id);

    public IQueryable<OrganizationMember> GetOrgMemberAsync(Guid userId, Guid orgId) =>
        FindByCondition(x => x.UserId == userId && x.OrgId == orgId);

    public IQueryable<OrganizationMember> GetMembersQuery(Guid orgId, string? search = null, OrganizationRoleEnum? role = null)
    {
        var query = FindByCondition(x => x.OrgId == orgId)
            .Include(x => x.User)
                .ThenInclude(u => u.AvatarFile)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(x =>
                x.User.Name.ToLower().Contains(term) ||
                x.User.Username.ToLower().Contains(term));
        }

        if (role.HasValue)
            query = query.Where(x => x.Role == role.Value);

        return query.OrderBy(x => x.JoinedAt);
    }

    public IQueryable<OrganizationMember> GetMemberById(Guid membershipId) =>
        FindByCondition(x => x.Id == membershipId)
            .Include(x => x.User)
                .ThenInclude(u => u.AvatarFile);

    public IQueryable<OrganizationMember> GetMembershipsByUserId(Guid userId) =>
        FindByCondition(x => x.UserId == userId)
            .Include(x => x.Organization)
                .ThenInclude(o => o.LogoFile)
            .OrderBy(x => x.JoinedAt);
}