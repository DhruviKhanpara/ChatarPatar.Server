using ChatarPatar.Infrastructure.Entities;
using ChatarPatar.Infrastructure.Persistence;
using ChatarPatar.Infrastructure.RepositoryContracts;
using Microsoft.EntityFrameworkCore;

namespace ChatarPatar.Infrastructure.Repositories;

internal class UserRepository : BaseRepository<User>, IUserRepository
{
    public UserRepository(AppDbContext context) : base(context) { }

    public IQueryable<User> GetByIdAsync(Guid id) => FindByCondition(x => x.Id == id).AsQueryable();

    public async Task<User?> GetUserByIdentifierAsync(string email, string username) => await FindByCondition(x => x.Email == email || x.Username == username).AsNoTracking().FirstOrDefaultAsync();

    public async Task<User?> GetUserByEmailAsync(string email) => await FindByCondition(x => x.Email == email).AsNoTracking().FirstOrDefaultAsync();
}

