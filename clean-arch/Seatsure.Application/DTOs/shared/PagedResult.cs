namespace Seatsure.Application.Bll.DTOs;

/// <summary> Offset Pagination {items, page, pageSize, totalCount}</summary>
public record PagedResult<T>(IEnumerable<T>items, int page, int pageSize, int totalCount);
