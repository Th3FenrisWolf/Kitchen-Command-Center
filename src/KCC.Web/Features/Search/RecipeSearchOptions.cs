namespace KCC.Web.Features.Search;

public class RecipeSearchOptions
{
    public TimeSpan RebuildDelay { get; set; } = TimeSpan.FromSeconds(2);
}
