namespace ChatarPatar.Common.Models;

public class PagedResult<T> where T : class
{
    public PagedResult() { }

    public PagedResult(List<T> items, int totalCount, int pageNumber, int pageSize)
    {
        Data = items;
        TotalCount = totalCount;
        PageNumber = pageNumber;
        PageSize = pageSize;
    }

    public List<T> Data { get; init; } = new();
    public int TotalCount { get; init; }
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
    public bool HasNextPage => PageNumber < TotalPages;
    public bool HasPreviousPage => PageNumber > 1;
}
