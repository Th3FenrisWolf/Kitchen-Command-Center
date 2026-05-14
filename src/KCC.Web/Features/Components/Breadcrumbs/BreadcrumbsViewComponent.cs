using KCC.Web.Features.Pages.Shared;
using Microsoft.AspNetCore.Mvc;

namespace KCC.Web.Features.Components.Breadcrumbs;

public class BreadcrumbsViewComponent(BreadcrumbService breadcrumbService) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(BasePageViewModel sourceViewModel)
    {
        if (sourceViewModel.ShowBreadcrumbs is false)
        {
            return Content(string.Empty);
        }

        var breadcrumbs = await breadcrumbService.BuildBreadcrumbsAsync(sourceViewModel.WebPageItemID);

        if (breadcrumbs.Count is 0)
        {
            return Content(string.Empty);
        }

        var viewModel = new BreadcrumbsViewModel { Links = breadcrumbs };

        return View("~/Features/Components/Breadcrumbs/Default.cshtml", viewModel);
    }
}
