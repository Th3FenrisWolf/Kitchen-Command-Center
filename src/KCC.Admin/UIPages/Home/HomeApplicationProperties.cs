using Kentico.Xperience.Admin.Base;

namespace KCC.Admin.UIPages.Home;

public sealed class HomeApplicationProperties : TemplateClientProperties
{
    public IReadOnlyList<FeaturedTile> FeaturedTiles { get; set; } = [];

    public IReadOnlyList<CategoryGroup> Categories { get; set; } = [];
}

public sealed record FeaturedTile(
    string Identifier,
    string Name,
    string IconName,
    string Url,
    string Description,
    IReadOnlyList<TileStat> Stats);

public sealed record TileStat(string Label, string Value);

public sealed record CategoryGroup(string Category, IReadOnlyList<Tile> Tiles);

public sealed record Tile(string Identifier, string Name, string IconName, string Url);
