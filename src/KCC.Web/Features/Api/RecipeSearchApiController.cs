using KCC.Web.Features.Search;
using Microsoft.AspNetCore.Mvc;

namespace KCC.Web.Features.Api;

[ApiController]
[Route("api/recipes")]
public class RecipeSearchApiController(IRecipeSearchService search) : ControllerBase
{
    [HttpGet("search")]
    public IActionResult Search(
        [FromQuery] string query = "",
        [FromQuery] string[] category = null,
        [FromQuery] string[] diet = null,
        [FromQuery] int timeMin = 0,
        [FromQuery] int timeMax = RecipeSearchCriteria.MaxTime,
        [FromQuery] string sort = "relevant",
        [FromQuery] int page = 0,
        [FromQuery] int pageSize = RecipeSearchCriteria.DefaultPageSize)
    {
        var results = search.Search(new()
        {
            Query = query,
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
