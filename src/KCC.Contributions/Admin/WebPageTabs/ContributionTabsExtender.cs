using KCC.Contributions.Admin.WebPageTabs;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Websites.UIPages;

[assembly: PageExtender(typeof(ContributionTabsExtender))]

namespace KCC.Contributions.Admin.WebPageTabs;

/// <remarks>
/// <see cref="UIPageAttribute"/> carries no visibility option and <c>WebPageLayout</c> is sealed with a
/// private navigation filter that knows only the platform's own slugs, so a tab registered against the
/// layout renders on every page in the channel. An extender is the supported way back: the invoker
/// calls it with the properties the layout has already built, so this only has to take items away.
///
/// Only <c>Navigation.Items</c> is touched. Both routes stay registered and are guarded by each page's
/// own <c>ValidatePage</c> and by the READ ACL check <c>WebPageBase</c> performs.
///
/// The permission attribute is here because this is the only type in the feature the platform collects
/// one from; <see cref="ContributionPermissions"/> records why.
/// </remarks>
[UIPermission(ContributionPermissions.ViewContributions, "View community contributions")]
[UIPermission(ContributionPermissions.ManageContributions, "Edit community contributions")]
public sealed class ContributionTabsExtender(IContributionPageLookup pageLookup) : PageExtender<WebPageLayout>
{
    private static readonly string[] ContributionSlugs =
        [RecipeContributionsPage.Slug, VariantContributionsPage.Slug];

    public override async Task<TemplateClientProperties> ConfigureTemplateProperties(
        TemplateClientProperties properties)
    {
        properties = await base.ConfigureTemplateProperties(properties);

        var identity = pageLookup.Identify(Page.WebPageIdentifier.WebPageItemID);

        var applicableSlug = identity switch
        {
            _ when identity.Is(ContributionContentTypes.Recipe) => RecipeContributionsPage.Slug,
            _ when identity.Is(ContributionContentTypes.RecipeVariant) => VariantContributionsPage.Slug,
            _ => string.Empty,
        };

        properties.Navigation.Items = properties.Navigation.Items
            .Where(item => !ContributionSlugs.Contains(item.Path) || item.Path == applicableSlug)
            .ToList();

        return properties;
    }
}
