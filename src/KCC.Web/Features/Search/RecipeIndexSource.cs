using KCC.Contributions;
using KCC.Web.Features.Providers;
using KCC.Web.Features.Recipes;
using Umbraco.Cms.Core.Web;

namespace KCC.Web.Features.Search;

public interface IRecipeIndexSource
{
    Task<IReadOnlyList<RecipeSearchDocument>> LoadAsync();
}

public class RecipeIndexSource(
    IUmbracoContextFactory umbracoContextFactory,
    IRecipeQueries recipes,
    IContributionStats contributionStats,
    IAuthorNameProvider authorNames) : IRecipeIndexSource
{
    public async Task<IReadOnlyList<RecipeSearchDocument>> LoadAsync()
    {
        IReadOnlyList<RecipePageData> pages;
        IReadOnlySet<string> styleTags;

        // A rebuild runs outside any request, and published URLs are built from an Umbraco context.
        using (umbracoContextFactory.EnsureUmbracoContext())
        {
            pages = recipes.GetPublishedRecipes();
            styleTags = recipes.GetStyleTagNames();
        }

        var stats = await contributionStats.GetAsync();
        var names = await authorNames.ResolveMany(RecipeSearchDocuments.AuthorKeys(pages));
        return pages.Select(page => RecipeSearchDocuments.From(page, stats, names, styleTags)).ToList();
    }
}
