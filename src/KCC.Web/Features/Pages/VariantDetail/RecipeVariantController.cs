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
    : RenderController(logger, compositeViewEngine, umbracoContextAccessor)
{
    // Route hijacking calls the synchronous Index unless it is hidden like this; the overload below serves the page.
    [NonAction]
    public sealed override IActionResult Index() => throw new NotSupportedException();

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
        viewModel.ResourceStrings = GetStrings();
        pageMetadata.Apply(variant, viewModel);

        return View("~/Features/Pages/VariantDetail/Index.cshtml", viewModel);
    }

    private Dictionary<string, string> GetStrings() => resourceStrings.GetManyOrDefault(
        "VariantDetail.Ingredients",
        "VariantDetail.VariantOf",
        "VariantDetail.By",
        "VariantDetail.CookMode",
        "VariantDetail.Close",
        "VariantDetail.Next",
        "VariantDetail.Previous",
        "VariantDetail.Step",
        "VariantDetail.Of",
        "VariantDetail.MarkDone",
        "VariantDetail.Done",
        "VariantDetail.Servings",
        "VariantDetail.StartTimer",
        "VariantDetail.Pause",
        "VariantDetail.Reset",
        "VariantDetail.ComingSoon",
        "VariantDetail.Prep",
        "VariantDetail.Cook",
        "VariantDetail.Count",
        "VariantDetail.Difficulty",
        "VariantDetail.DifficultyEasy",
        "VariantDetail.DifficultyMedium",
        "VariantDetail.DifficultyHard",
        "VariantDetail.Makes",
        "VariantDetail.Fewer",
        "VariantDetail.More",
        "VariantDetail.ToTaste",
        "VariantDetail.Nutrition",
        "VariantDetail.PerServing",
        "VariantDetail.Calories",
        "VariantDetail.Protein",
        "VariantDetail.Carbs",
        "VariantDetail.Fat",
        "VariantDetail.SaturatedFat",
        "VariantDetail.Fiber",
        "VariantDetail.Sugar",
        "VariantDetail.Sodium",
        "VariantDetail.NutritionNotProvided",
        "VariantDetail.Instructions",
        "VariantDetail.CookNotes",
        "VariantDetail.CookNotesComingSoon",
        "VariantDetail.RatingsReviews",
        "VariantDetail.ReviewsComingSoon",
        "VariantDetail.OtherVariants",
        "VariantDetail.WriteReview",
        "VariantDetail.YourReview",
        "VariantDetail.SubmitReview",
        "VariantDetail.EditReview",
        "VariantDetail.DeleteReview",
        "VariantDetail.LogInToReview",
        "VariantDetail.NoReviewsYet",
        "VariantDetail.ReviewCount",
        "VariantDetail.AddCookNote",
        "VariantDetail.CookNotePlaceholder",
        "VariantDetail.NoCookNotesYet",
        "VariantDetail.DeleteNote",
        "VariantDetail.ICookedThis",
        "VariantDetail.CookedCount",
        "VariantDetail.LoadMore",
        "VariantDetail.Reviews",
        "VariantDetail.NoRatingsYet",
        "VariantDetail.TimesCooked");
}
