namespace TalentFlow.Application.Common;

/// <summary>
/// Standard pagination parameters for list queries.
/// </summary>
public class PaginationParams
{
    private const int MaxPageSize = 50;
    private int _pageSize = 10;

    public int Page { get; set; } = 1;

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value > MaxPageSize ? MaxPageSize : value;
    }

    public string? SortBy { get; set; }
    public bool SortDescending { get; set; }
    public string? Search { get; set; }
}
