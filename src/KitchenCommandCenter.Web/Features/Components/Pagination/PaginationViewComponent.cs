using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace KitchenCommandCenter.Web.Features.Components.Pagination;

public class PaginationViewComponent : ViewComponent
{
    public Task<IViewComponentResult> InvokeAsync(PaginationParameters paginationParameters)
    {
        if (paginationParameters == null || paginationParameters.TotalResults <= paginationParameters.PageSize)
        {
            return Task.FromResult<IViewComponentResult>(Content(string.Empty));
        }

        return Task.FromResult<IViewComponentResult>(View("~/Features/Components/Pagination/Pagination.cshtml", paginationParameters));
    }
}