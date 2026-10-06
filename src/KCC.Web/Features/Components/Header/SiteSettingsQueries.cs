using KCC.Web.Features.Models.Generated;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Blocks;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace KCC.Web.Features.Components.Header;

public class SiteSettingsQueries(IPublishedContentQuery contentQuery)
{
    public HeaderNavigation GetHeaderNavigation()
    {
        var settings = contentQuery.ContentAtRoot().OfType<SiteSettings>().FirstOrDefault();
        return settings is null
            ? HeaderNavigation.Empty
            : new HeaderNavigation(Entries(settings.MainNav), Entries(settings.UtilityNav));
    }

    public NavSettings GetNavSettings()
    {
        var settings = contentQuery.ContentAtRoot().OfType<SiteSettings>().FirstOrDefault();
        return settings is null
            ? NavSettings.Empty
            : new NavSettings(
                Names(settings.NavMeals),
                Names(settings.NavDiets),
                QuickPicks(settings.NavQuickPicks),
                (settings.NavSearchSuggestions ?? []).Select(phrase => phrase.Trim()).Where(phrase => phrase.Length > 0).ToList(),
                string.IsNullOrWhiteSpace(settings.NavRecipesNote) ? null : settings.NavRecipesNote.Trim());
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

    private static List<string> Names(IEnumerable<IPublishedContent> picks) => (picks ?? []).Select(pick => pick.Name).ToList();

    private static List<NavQuickPickSetting> QuickPicks(BlockListModel blocks) =>
        (blocks ?? Enumerable.Empty<BlockListItem>())
            .Select(block => block.Content switch
            {
                NavPreset preset => new NavQuickPickSetting(preset.Preset, preset.Label, null, null),
                NavQuickLink link => new NavQuickPickSetting(null, link.Label, link.Link?.Url, link.Link?.Target),
                _ => null,
            })
            .Where(pick => pick is not null)
            .ToList();

    private static NavTarget Target(Link link) =>
        link?.Url is { Length: > 0 } url ? new NavTarget(link.Name ?? string.Empty, url, link.Target) : null;
}
