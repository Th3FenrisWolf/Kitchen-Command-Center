using KCC.Web.Features.Models.Generated;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Extensions;

namespace KCC.Web.Features.Sitemap;

public sealed record SitemapCandidate(string Url, string ContentTypeAlias, bool ExcludeFromSitemap);

public interface ISitemapPages
{
    IEnumerable<string> Urls();
}

public static class SitemapFilter
{
    private static readonly HashSet<string> ExcludedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "accountPage",
        "accountSettingsPage",
        "addVariantPage",
        "createRecipePage",
        "loginPage",
        "registrationCompletePage",
    };

    public static IEnumerable<string> Urls(IEnumerable<SitemapCandidate> candidates) =>
        candidates
            .Where(candidate => !candidate.ExcludeFromSitemap && !ExcludedTypes.Contains(candidate.ContentTypeAlias))
            .Select(candidate => candidate.Url.ToLowerInvariant())
            .Distinct(StringComparer.Ordinal);
}

public class SitemapPages(IPublishedContentQuery contentQuery) : ISitemapPages
{
    public IEnumerable<string> Urls() => SitemapFilter.Urls(
        contentQuery.ContentAtRoot()
            .OfType<HomePage>()
            .SelectMany(home => new IPublishedContent[] { home }.Concat(home.Descendants()))
            .OfType<IMetadata>()
            .Select(page => new SitemapCandidate(page.Url(), page.ContentType.Alias, page.ExcludeFromSitemap)));
}
