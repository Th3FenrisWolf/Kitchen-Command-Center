using KCC;
using KCC.ResourceStrings.Data;
using KCC.Web.Features.Models.Constants;
using KCC.Web.Features.Pages.CreateRecipe;
using Kentico.Content.Web.Mvc.Routing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[assembly: RegisterWebPageRoute(
    CreateRecipePage.CONTENT_TYPE_NAME,
    typeof(CreateRecipeController),
    WebsiteChannelNames = [XperienceConstants.WebsiteChannelName]
)]

namespace KCC.Web.Features.Pages.CreateRecipe;

[Authorize]
public class CreateRecipeController(IResourceStringInfoProvider resourceStrings) : Controller
{
    public IActionResult Index()
    {
        var viewModel = new CreateRecipeViewModel
        {
            ResourceStrings = GetStrings(),
        };

        return View("~/Features/Pages/CreateRecipe/Index.cshtml", viewModel);
    }

    private Dictionary<string, string> GetStrings() => resourceStrings.GetManyOrDefault(
        "CreateRecipe.CreateRecipe"
    );
}
