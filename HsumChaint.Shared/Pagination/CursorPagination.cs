using System.Text.Json.Serialization;

namespace HsumChaint.Shared;

public class CursorPaginationRequest
{
    public int? LastId { get; set; }
    public int PageSize { get; set; } = 15;
}

public class CursorPagination
{
    public int? NextCursor { get; init; }
    public bool HasNextPage { get; init; }
    public int PageSize { get; init; }

    [JsonConstructor]
    public CursorPagination(int? nextCursor, bool hasNextPage, int pageSize)
    {
        NextCursor = nextCursor;
        HasNextPage = hasNextPage;
        PageSize = pageSize;
    }
}

public class CursorPagedResult<T> : Result
{
    public List<T> Data { get; init; }
    public CursorPagination Pagination { get; init; }

    [JsonConstructor]
    public CursorPagedResult(bool isSuccess, string message, List<T> data, CursorPagination pagination)
        : base(isSuccess, message)
    {
        Data = data;
        Pagination = pagination;
    }

    public static CursorPagedResult<T> Success(List<T> data, CursorPagination pagination, string message = "Success")
        => new(true, message, data, pagination);

    public new static CursorPagedResult<T> Failure(string message)
        => new(false, message, new List<T>(), new CursorPagination(null, false, 0));
}
