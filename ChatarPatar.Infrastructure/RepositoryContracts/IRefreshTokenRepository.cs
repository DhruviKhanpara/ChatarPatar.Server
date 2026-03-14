using ChatarPatar.Infrastructure.Entities;
using System.Linq.Expressions;

namespace ChatarPatar.Infrastructure.RepositoryContracts;

public interface IRefreshTokenRepository
{
    IQueryable<RefreshToken> FindByCondition(Expression<Func<RefreshToken, bool>> expression);
    Task AddAsync(RefreshToken entity);
    void Update(RefreshToken existingEntity, RefreshToken entity);
}
