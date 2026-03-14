using ChatarPatar.Infrastructure.Entities;
using ChatarPatar.Infrastructure.Persistence;
using ChatarPatar.Infrastructure.RepositoryContracts;
using System.Linq.Expressions;

namespace ChatarPatar.Infrastructure.Repositories;

internal class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly AppDbContext _context;

    public RefreshTokenRepository(AppDbContext context)
    {
        _context = context;
    }

    public IQueryable<RefreshToken> FindByCondition(Expression<Func<RefreshToken, bool>> expression) => _context.RefreshTokens.Where(expression).AsQueryable();
    public async Task AddAsync(RefreshToken entity) => await _context.RefreshTokens.AddAsync(entity);
    public void Update(RefreshToken existingEntity, RefreshToken entity) => _context.Entry(existingEntity).CurrentValues.SetValues(entity);
}
