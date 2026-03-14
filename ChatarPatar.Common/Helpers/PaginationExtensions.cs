using ChatarPatar.Common.AppExceptions.CustomExceptions;

namespace ChatarPatar.Common.Helpers;

public static class PaginationExtensions
{
    public static IQueryable<T> PaginateOffset<T>(this IQueryable<T> query, int pageSize = 10, int pageNumber = 1)
    {
        if (pageSize <= 0 || pageNumber <= 0)
            throw new InvalidDataAppException("Pagination params");

        pageSize = Math.Min(pageSize, 100);

        return query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize);
    }
}
