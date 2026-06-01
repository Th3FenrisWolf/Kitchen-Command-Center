using CMS.ContentEngine;
using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using Microsoft.AspNetCore.Mvc;

namespace KCC.Web.Features.Components.Header;

public class HeaderViewComponent(
    IContentRetriever contentRetriever,
    ITaxonomyRetriever taxonomyRetriever,
    IPreferredLanguageRetriever preferredLanguageRetriever
) : ViewComponent
{
    private const string AuthStatusTaxonomyName = "AuthStatus";
    private const string AuthenticatedTagName = "Authenticated";
    private const string UnauthenticatedTagName = "Unauthenticated";

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var headerNav = (await contentRetriever.RetrieveContent<HeaderNavigation>(
            new RetrieveContentParameters { LinkedItemsMaxLevel = 3 },
            query => query.TopN(1),
            new($"{nameof(HeaderViewComponent)}|{nameof(InvokeAsync)}")
        )).FirstOrDefault();

        var currentStatusTagId = await GetCurrentAuthStatusTagId();

        var mainNavItems = RetrieveNavItems(headerNav?.MainNavItems, currentStatusTagId);
        var utilityNavItems = RetrieveNavItems(headerNav?.UtilityNavItems, currentStatusTagId);

        var viewModel = new HeaderViewModel
        {
            Logo = headerNav.Logo.FirstOrDefault(),
            MainNavItems = MapPageLinks(mainNavItems),
            UtilityNavItems = MapPageLinks(utilityNavItems),
        };

        return View("~/Features/Components/Header/Header.cshtml", viewModel);
    }

    private static IEnumerable<NavItem> RetrieveNavItems(IEnumerable<IContentItemFieldsSource> sourceItems, Guid? currentStatusTagId)
    {
        var navItems = sourceItems.OfType<NavItem>();
        var navLinks = sourceItems.OfType<LinkItem>();

        var filteredItems = FilterByAuth(navItems, currentStatusTagId);
        return filteredItems;
    }

    private async Task<Guid?> GetCurrentAuthStatusTagId()
    {
        var language = preferredLanguageRetriever.Get();
        var taxonomy = await taxonomyRetriever.RetrieveTaxonomy(AuthStatusTaxonomyName, language);
        var tagName = User.Identity?.IsAuthenticated == true
            ? AuthenticatedTagName
            : UnauthenticatedTagName;

        return taxonomy?.Tags
            ?.FirstOrDefault(t => string.Equals(t.Name, tagName, StringComparison.OrdinalIgnoreCase))
            ?.Identifier;
    }

    private static IEnumerable<NavItem> FilterByAuth(IEnumerable<NavItem> navItems, Guid? currentStatusTagId)
    {
        if (navItems is null)
        {
            return [];
        }

        return navItems.Where(item =>
        {
            var showWhen = item.ShowWhen?.ToList();
            if (showWhen is null || showWhen.Count == 0)
            {
                return true;
            }

            if (currentStatusTagId is null)
            {
                return false;
            }

            return showWhen.Any(t => t.Identifier == currentStatusTagId.Value);
        });
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
