namespace KCC.Web.Features.Search;

public interface IRecipeSearchService
{
    RecipeSearchResults Search(RecipeSearchCriteria criteria);
}
