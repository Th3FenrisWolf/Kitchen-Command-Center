using KCC.Web.Features.Dictionary;
using KCC.Web.Features.Models.Generated;
using KCC.Web.Features.Pages.Account;
using KCC.Web.Features.Pages.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;

namespace KCC.Web.Features.Pages.CreateRecipe;

public class CreateRecipePageController(
    ILogger<RenderController> logger,
    ICompositeViewEngine compositeViewEngine,
    IUmbracoContextAccessor umbracoContextAccessor,
    IMemberManager memberManager,
    AccountPageQueries accountPages,
    IResourceStringProvider resourceStrings,
    PageMetadata pageMetadata)
    : AsyncRenderController(logger, compositeViewEngine, umbracoContextAccessor)
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (CurrentPage is not CreateRecipePage page)
        {
            return NotFound();
        }

        if (await memberManager.GetCurrentMemberAsync() is null)
        {
            return SignInRedirect.To(accountPages.GetUrls().Login, Request);
        }

        var viewModel = new CreateRecipeViewModel { ResourceStrings = resourceStrings.GetGroup("CreateRecipe") };
        pageMetadata.Apply(page, viewModel);

        return View("~/Features/Pages/CreateRecipe/Index.cshtml", viewModel);
    }
}
