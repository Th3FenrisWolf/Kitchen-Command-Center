using KCC.Web.Features.Models.Generated;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Blocks;

namespace KCC.Web.Features.Components.Header;

public interface ISiteSettingsQueries
{
    HeaderNavigation GetHeaderNavigation();
}

public class SiteSettingsQueries(IPublishedContentQuery contentQuery) : ISiteSettingsQueries
{
    public HeaderNavigation GetHeaderNavigation()
    {
        var settings = contentQuery.ContentAtRoot().OfType<SiteSettings>().FirstOrDefault();
        return settings is null
            ? HeaderNavigation.Empty
            : new HeaderNavigation(Entries(settings.MainNav), Entries(settings.UtilityNav));
    }

    private static List<NavEntry> Entries(BlockListModel blocks) =>
        (blocks ?? Enumerable.Empty<BlockListItem>())
            .Select(block => block.Content switch
            {
                NavLink link => new NavEntry(link.DisplayText ?? string.Empty, link.ShowWhen, Target(link.Link), []),
                NavGroup group => new NavEntry(
                    group.DisplayText ?? string.Empty,
                    group.ShowWhen,
                    null,
                    (group.Links ?? []).Select(Target).Where(target => target is not null).ToList()),
                _ => null,
            })
            .Where(entry => entry is not null)
            .ToList();

    private static NavTarget Target(Link link) =>
        link?.Url is { Length: > 0 } url ? new NavTarget(link.Name ?? string.Empty, url, link.Target) : null;
}
