using ChatarPatar.Infrastructure.Entities;

namespace ChatarPatar.Infrastructure.RepositoryContracts;

public interface IUserRepository : IBaseRepository<User>
{
    IQueryable<User> GetById(Guid id);
    Task<User?> GetUserByIdentifierAsync(string email, string username);
    Task<User?> GetUserByEmailAsync(string email);
}

