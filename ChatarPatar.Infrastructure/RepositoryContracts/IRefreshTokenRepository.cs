using ChatarPatar.Infrastructure.Entities;
using System.Linq.Expressions;

namespace ChatarPatar.Infrastructure.RepositoryContracts;

public interface IRefreshTokenRepository
{
    IQueryable<RefreshToken> FindByCondition(Expression<Func<RefreshToken, bool>> expression);
    IQueryable<RefreshToken> FindActiveRefreshToken(string token);
    IQueryable<RefreshToken> GetActiveRefreshTokensByUserId(Guid userId);
    Task AddAsync(RefreshToken entity);
    void Update(RefreshToken existingEntity, RefreshToken entity);
}
