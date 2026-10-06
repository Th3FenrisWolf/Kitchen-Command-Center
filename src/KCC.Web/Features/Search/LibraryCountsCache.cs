namespace KCC.Web.Features.Search;

public class LibraryCountsCache(RecipeIndex index, IRecipeSearchService search)
{
    private Counted counted;

    public LibraryCounts Current()
    {
        // Read before searching, so counts taken from a newer snapshot are only ever filed under an older version, which
        // the next call recounts.
        var version = index.Version;
        var last = Volatile.Read(ref counted);
        if (last?.Version == version)
        {
            return last.Counts;
        }

        var library = search.Search(new RecipeSearchCriteria());
        var quick = search.Search(new RecipeSearchCriteria { TimeMax = LibraryCounts.QuickTimeMax });
        var counts = new LibraryCounts(library.Total, library.Facets.Category, library.Facets.Diet, quick.Total);
        Volatile.Write(ref counted, new Counted(version, counts));
        return counts;
    }

    private sealed record Counted(long Version, LibraryCounts Counts);
}

public sealed record LibraryCounts(
    int Total,
    IReadOnlyDictionary<string, int> Categories,
    IReadOnlyDictionary<string, int> Diets,
    int UnderThirtyMinutes)
{
    public const int QuickTimeMax = 30;
}
