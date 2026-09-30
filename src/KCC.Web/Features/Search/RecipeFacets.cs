using Lucene.Net.Facet;

namespace KCC.Web.Features.Search;

public static class RecipeFacets
{
    // The index writer and the drill-down query must encode facet terms the same way, so both take their
    // configuration from here.
    public static FacetsConfig Config()
    {
        var config = new FacetsConfig();
        config.SetMultiValued(RecipeSearchConstants.FacetCategory, true);
        config.SetMultiValued(RecipeSearchConstants.FacetDiet, true);
        return config;
    }
}
