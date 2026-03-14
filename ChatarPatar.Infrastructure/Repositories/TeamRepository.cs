using ChatarPatar.Infrastructure.Entities;
using ChatarPatar.Infrastructure.Persistence;
using ChatarPatar.Infrastructure.RepositoryContracts;

namespace ChatarPatar.Infrastructure.Repositories;

internal class TeamRepository : BaseSoftDeleteRepository<Team>, ITeamRepository
{
    public TeamRepository(AppDbContext context) : base(context) { }
}

