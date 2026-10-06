namespace KCC.Web.Features.Components.Header;

public static class NavPresets
{
    public const string UnderThirtyMinutes = "Under 30 minutes";
    public const string TopRated = "Top rated";
    public const string MostVariants = "Most variants";
    public const string Newest = "Newest";
    public const string SurpriseMe = "Surprise me";

    public static IReadOnlyList<string> All { get; } = [UnderThirtyMinutes, TopRated, MostVariants, Newest, SurpriseMe];
}
