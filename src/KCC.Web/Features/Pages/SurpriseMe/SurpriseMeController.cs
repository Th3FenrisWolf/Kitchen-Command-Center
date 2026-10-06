using KCC.Web.Features.Search;
using Microsoft.AspNetCore.Mvc;

namespace KCC.Web.Features.Pages.SurpriseMe;

public class SurpriseMeController(LibraryCountsCache libraryCounts, IRecipeSearchService search) : Controller
{
    [HttpGet("surprise-me")]
    public IActionResult Index()
    {
        var total = libraryCounts.Current().Total;
        var pick = total == 0
            ? null
            : search.Search(new RecipeSearchCriteria { Page = Random.Shared.Next(total), PageSize = 1 }).Results.FirstOrDefault();

        return Redirect(pick?.Slug ?? "/");
    }
}
