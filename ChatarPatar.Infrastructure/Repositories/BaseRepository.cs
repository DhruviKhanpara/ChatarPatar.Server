using ChatarPatar.Infrastructure.Persistence;
using ChatarPatar.Infrastructure.RepositoryContracts;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace ChatarPatar.Infrastructure.Repositories;

internal class BaseRepository<T> : IBaseRepository<T> where T : class
{
    protected readonly AppDbContext _context;
    private readonly DbSet<T> _dbSet;

    public BaseRepository(AppDbContext context)
    {
        _context = context;
        _dbSet = _context.Set<T>();
    }

    public IQueryable<T> GetAll() => _dbSet.AsQueryable();

    public IQueryable<T> FindByCondition(Expression<Func<T, bool>> expression) => _dbSet.Where(expression).AsQueryable();

    public Task<bool> AnyAsync(Expression<Func<T, bool>> expression) => _dbSet.AnyAsync(expression);

    public async Task AddAsync(T entity) => await _dbSet.AddAsync(entity);

    //public void Update(T existingEntity, T entity) => _context.Entry(existingEntity).CurrentValues.SetValues(entity);
    public void Update(T existingEntity, T entity) => _dbSet.Update(entity);

    public void Remove(T entity) => _dbSet.Remove(entity);

    public void RemoveRange(List<T> entities) => _dbSet.RemoveRange(entities);
}
