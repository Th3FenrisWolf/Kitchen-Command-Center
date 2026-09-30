namespace KCC.Web.Features.Search;

public class RecipeSearchOptions
{
    public TimeSpan RebuildDelay { get; set; } = TimeSpan.FromSeconds(2);

    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(30);

    public TimeSpan MaxRebuildWait { get; set; } = TimeSpan.FromSeconds(10);
}
