using Kentico.Content.Web.Mvc;
using Microsoft.AspNetCore.Mvc;

namespace KCC.Web.Features.Components.Header;

public class HeaderViewComponent(
    IContentRetriever contentRetriever
) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var headerNav = (await contentRetriever.RetrieveContent<HeaderNavigation>(
            new RetrieveContentParameters { LinkedItemsMaxLevel = 3 },
            query => query.TopN(1),
            new($"{nameof(HeaderViewComponent)}|{nameof(InvokeAsync)}")
        )).FirstOrDefault();

        var viewModel = new HeaderViewModel
        {
            Logo = headerNav.Logo.FirstOrDefault(),
            MainNavItems = MapPageLinks(headerNav.MainNavItems),
            UtilityNavItems = MapPageLinks(headerNav.UtilityNavItems),
        };

        return View("~/Features/Components/Header/Header.cshtml", viewModel);
    }

    private static IEnumerable<HeaderNavItem> MapPageLinks(IEnumerable<NavItem> navItems)
    {
        if (navItems?.Any() is not true)
        {
            return [];
        }

        return navItems.Select(navItem => new HeaderNavItem
        {
            DisplayText = navItem.DisplayText,
            SubLinks = navItem.SubLinks.Select(subLink => subLink.MapToPageLink()),
        });
    }
}
