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

    private static IEnumerable<IContentItemFieldsSource> RetrieveNavItems(IEnumerable<IContentItemFieldsSource> sourceItems, Guid? currentStatusTagId)
    {
        var navItems = sourceItems.OfType<NavItem>().Where(item =>
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

        var navLinks = sourceItems.OfType<NavLink>().Where(item =>
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

        return sourceItems.Where(item =>
            (item is NavItem ni && navItems.Contains(ni)) ||
            (item is NavLink nl && navLinks.Contains(nl)));
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

    private static IEnumerable<HeaderNavItem> MapPageLinks(IEnumerable<IContentItemFieldsSource> items)
    {
        if (items?.Any() is not true)
        {
            return [];
        }

        return items.Select<IContentItemFieldsSource, HeaderNavItem>(item => item switch
        {
            NavItem navItem => new HeaderNavItem
            {
                DisplayText = navItem.DisplayText,
                SubLinks = navItem.SubLinks.Select(subLink => subLink.MapToPageLink()),
            },
            NavLink navLink => new HeaderNavItem
            {
                DisplayText = navLink.DisplayText,
                Url = navLink.MapToPageLink().Url,
                Target = navLink.Target,
            },
            _ => null,
        }).Where(navItem => navItem is not null);
    }
}
