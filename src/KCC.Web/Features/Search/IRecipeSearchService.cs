namespace KCC.Web.Features.Search;

/// <summary>Queries the recipe Lucene index for full-text matches with facet, time, sort and paging support.</summary>
public interface IRecipeSearchService
{
    /// <summary>Runs a recipe search for the supplied criteria.</summary>
    /// <param name="criteria">The search criteria (query, facet filters, time range, sort and paging).</param>
    /// <returns>The requested page of hits together with facet counts and the optional spotlight recipe.</returns>
    RecipeSearchResults Search(RecipeSearchCriteria criteria);
}
