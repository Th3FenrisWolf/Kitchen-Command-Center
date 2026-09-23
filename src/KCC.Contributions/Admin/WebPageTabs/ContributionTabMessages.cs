namespace KCC.Contributions.Admin.WebPageTabs;

/// <remarks>
/// Phrased rather than keyed. Kentico resolves a validation message through its own localization, and
/// this project holds those strings as database rows whose English values are populated per
/// environment — so a key renders as itself wherever it has not been. These messages are reachable
/// only by typing a tab's URL against a page of the wrong content type.
/// </remarks>
internal static class ContributionTabMessages
{
    public const string NotARecipe = "This page is not a recipe, so it has no community contributions.";

    public const string NotAVariant = "This page is not a recipe variant, so it has no community contributions.";

    public const string ReviewNotOnThisVariant = "This review was left against a different variant.";

    public const string NoteNotOnThisVariant = "This cook note was left against a different variant.";
}
