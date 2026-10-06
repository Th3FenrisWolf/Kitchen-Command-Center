namespace KCC.Web.Features.Components.Header;

public sealed record NavSettings(
    IReadOnlyList<string> Meals,
    IReadOnlyList<string> Diets,
    IReadOnlyList<NavQuickPickSetting> QuickPicks,
    IReadOnlyList<string> SearchSuggestions,
    string RecipesNote)
{
    public static NavSettings Empty { get; } = new([], [], [], [], null);
}

public sealed record NavQuickPickSetting(string Preset, string Label, string Url, string Target);
