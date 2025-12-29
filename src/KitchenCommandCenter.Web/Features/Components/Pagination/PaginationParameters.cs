using System;

namespace KitchenCommandCenter.Web.Features.Components.Pagination;

public class PaginationParameters
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalResults { get; set; }
    public string Url { get; set; } = string.Empty;
    public int GroupSize { get; set; } = 10;
    public int TotalPages => (int)Math.Ceiling(TotalResults / (double)PageSize);
    public int CurrentGroup => (Page - 1) / GroupSize;
    public int StartPage => (CurrentGroup * GroupSize) + 1;
    public int EndPage => Math.Min(StartPage + GroupSize - 1, TotalPages);
    public string HashFragment { get; set; } = string.Empty;
}