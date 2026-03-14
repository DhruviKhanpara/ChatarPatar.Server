using System.Linq.Expressions;

namespace ChatarPatar.Infrastructure.RepositoryContracts;

public interface IBaseRepository<T> where T : class
{
    IQueryable<T> GetAll();
    IQueryable<T> FindByCondition(Expression<Func<T, bool>> expression);
    Task<bool> AnyAsync(Expression<Func<T, bool>> expression);
    Task AddAsync(T entity);
    void Update(T existingEntity, T entity);
    void Remove(T entity);
    void RemoveRange(List<T> entities);
}
