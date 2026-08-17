namespace Seatsure.BLL.DTOs.Common;

/// <summary>Offset-pagination envelope. Matches README §3.5: { items, page, pageSize, totalCount }.</summary>
public record PagedResult<T>(IEnumerable<T> Items, int Page, int PageSize, int TotalCount);
