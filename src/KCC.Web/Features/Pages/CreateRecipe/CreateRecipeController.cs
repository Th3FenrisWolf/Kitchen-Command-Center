using KCC;
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
public class CreateRecipeController : Controller
{
    public IActionResult Index()
    {
        var viewModel = new CreateRecipeViewModel();

        return View("~/Features/Pages/CreateRecipe/Index.cshtml", viewModel);
    }
}
