namespace BadCourt.SharedKernel;

/// <summary>
/// One page of a larger result set, together with what a caller needs to ask for the next
/// one. Queries build this from a count and a page of rows; it holds no query of its own,
/// so the read side stays free to use Dapper or EF as it prefers.
/// </summary>
/// <typeparam name="T">The row type, always a DTO on the read side.</typeparam>
public sealed class PagedList<T>
{
    public PagedList(IReadOnlyList<T> items, int page, int pageSize, int totalCount)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentOutOfRangeException.ThrowIfLessThan(page, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
        ArgumentOutOfRangeException.ThrowIfNegative(totalCount);

        Items = items;
        Page = page;
        PageSize = pageSize;
        TotalCount = totalCount;
    }

    public IReadOnlyList<T> Items { get; }

    /// <summary>The one-based number of this page.</summary>
    public int Page { get; }

    public int PageSize { get; }

    /// <summary>Rows matching the query across every page, not just this one.</summary>
    public int TotalCount { get; }

    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

    public bool HasPreviousPage => Page > 1;

    public bool HasNextPage => Page < TotalPages;
}
