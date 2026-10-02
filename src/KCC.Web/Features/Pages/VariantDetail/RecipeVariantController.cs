using KCC.Contributions;
using KCC.Web.Features.Components.Breadcrumbs;
using KCC.Web.Features.Dictionary;
using KCC.Web.Features.Models.Generated;
using KCC.Web.Features.Pages.Shared;
using KCC.Web.Features.Providers;
using KCC.Web.Features.Recipes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;

namespace KCC.Web.Features.Pages.VariantDetail;

public class RecipeVariantController(
    ILogger<RenderController> logger,
    ICompositeViewEngine compositeViewEngine,
    IUmbracoContextAccessor umbracoContextAccessor,
    IRecipeQueries recipes,
    IContributionStats contributionStats,
    IContributionReads contributionReads,
    IAuthorNameProvider authorNames,
    IMemberManager memberManager,
    BreadcrumbService breadcrumbs,
    IResourceStringProvider resourceStrings,
    PageMetadata pageMetadata)
    : AsyncRenderController(logger, compositeViewEngine, umbracoContextAccessor)
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (CurrentPage is not RecipeVariant variant || recipes.GetVariantPage(variant) is not { } page)
        {
            return NotFound();
        }

        var member = await memberManager.GetCurrentMemberAsync();
        var viewModel = VariantDetailMapping.Map(
            page,
            await contributionStats.GetAsync(),
            await authorNames.ResolveMany(VariantDetailMapping.AuthorKeys(page)),
            hasCooked: member is not null && await contributionReads.HasCookedAsync(variant.Key, member.Key),
            isAuthenticated: member is not null);
        viewModel.Breadcrumbs = breadcrumbs.Build(variant);
        viewModel.ResourceStrings = resourceStrings.GetGroup("VariantDetail");
        pageMetadata.Apply(variant, viewModel);

        return View("~/Features/Pages/VariantDetail/Index.cshtml", viewModel);
    }
}
