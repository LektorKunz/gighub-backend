namespace GigHub.Api.Dtos;

/// <summary>
/// Genbrugelig konvolut til paginerede lister (introduceret med søgning/filtrering i gang 05,
/// se GET /api/events?page=&amp;pageSize= i design-brief.md).
/// </summary>
public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
