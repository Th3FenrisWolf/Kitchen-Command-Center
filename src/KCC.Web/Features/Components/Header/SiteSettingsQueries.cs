using KCC.Web.Features.Models.Generated;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.Blocks;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace KCC.Web.Features.Components.Header;

public class SiteSettingsQueries(IPublishedContentQuery contentQuery)
{
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
}
