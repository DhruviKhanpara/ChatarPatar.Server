namespace ChatarPatar.Common.Models;

public abstract class PaginationResult<T>
{
    public List<T> Data { get; init; } = new();
}

public sealed class PagedResult<T> : PaginationResult<T>
{
    public PagedResult() { }

    public PagedResult(List<T> items, int totalCount, int pageNumber, int pageSize)
    {
        Data = items;
        TotalCount = totalCount;
        PageNumber = pageNumber;
        PageSize = pageSize;
    }

    public int TotalCount { get; init; }
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
    public bool HasNextPage => PageNumber < TotalPages;
    public bool HasPreviousPage => PageNumber > 1;
}

public sealed class CursorPagedResult<T> : PaginationResult<T>
{
    public CursorPagedResult(List<T> items, bool hasMore, long? nextSequence = null)
    {
        Data = items;
        HasMore = hasMore;
        NextSequence = nextSequence;
    }

    public bool HasMore { get; init; }
    public long? NextSequence { get; init; }
}
