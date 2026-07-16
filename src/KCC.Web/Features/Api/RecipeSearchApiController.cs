using KCC.Web.Features.Search;
using Microsoft.AspNetCore.Mvc;

namespace KCC.Web.Features.Api;

/// <summary>Read API for the recipe search page (full-text, facets, sort, paging).</summary>
[ApiController]
[Route("api/recipes")]
public class RecipeSearchApiController(IRecipeSearchService search) : ControllerBase
{
    /// <summary>Runs a recipe search and returns the shared response envelope.</summary>
    /// <param name="q">Free-text query.</param>
    /// <param name="category">Selected category facet values (repeatable).</param>
    /// <param name="diet">Selected dietary facet values (repeatable).</param>
    /// <param name="timeMin">Minimum fastest-time minutes.</param>
    /// <param name="timeMax">Maximum fastest-time minutes.</param>
    /// <param name="sort">Sort key: relevant|rated|variants|recent.</param>
    /// <param name="page">Zero-based page index.</param>
    /// <param name="pageSize">Page size.</param>
    /// <returns>The search response envelope (results, facets, total, spotlight).</returns>
    [HttpGet("search")]
    public IActionResult Search(
        [FromQuery] string q = "",
        [FromQuery(Name = "category")] string[] category = null,
        [FromQuery(Name = "diet")] string[] diet = null,
        [FromQuery] int timeMin = 0,
        [FromQuery] int timeMax = RecipeSearchCriteria.MaxTime,
        [FromQuery] string sort = "relevant",
        [FromQuery] int page = 0,
        [FromQuery] int pageSize = RecipeSearchCriteria.DefaultPageSize)
    {
        var results = search.Search(new RecipeSearchCriteria
        {
            Query = q,
            Categories = category ?? [],
            Diets = diet ?? [],
            TimeMin = timeMin,
            TimeMax = timeMax,
            Sort = sort,
            Page = page,
            PageSize = pageSize,
        });

        return Ok(RecipeSearchResponseMapper.ToResponse(results));
    }
}
