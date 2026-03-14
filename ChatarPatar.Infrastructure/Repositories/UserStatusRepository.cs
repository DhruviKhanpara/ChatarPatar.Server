using ChatarPatar.Infrastructure.Entities;
using ChatarPatar.Infrastructure.Persistence;
using ChatarPatar.Infrastructure.Repositories;

internal class UserStatusRepository : BaseRepository<UserStatus>, IUserStatusRepository
{
    public UserStatusRepository(AppDbContext context) : base(context) { }
}