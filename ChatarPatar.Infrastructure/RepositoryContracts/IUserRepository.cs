using ChatarPatar.Infrastructure.Entities;

namespace ChatarPatar.Infrastructure.RepositoryContracts;

public interface IUserRepository : IBaseRepository<User>
{
    IQueryable<User> GetByIdAsync(Guid id);
}

