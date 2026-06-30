using CMS.ContentEngine;
using CMS.Websites;
using KCC;
using KCC.Contributions.Data;
using KCC.ResourceStrings.Data;
using KCC.Web.Features.Components.Breadcrumbs;
using KCC.Web.Features.Extensions;
using KCC.Web.Features.Helpers;
using KCC.Web.Features.Members;
using KCC.Web.Features.Models.Common;
using KCC.Web.Features.Models.Constants;
using KCC.Web.Features.Pages.Shared;
using KCC.Web.Features.Pages.VariantDetail;
using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

[assembly: RegisterWebPageRoute(
    RecipeVariant.CONTENT_TYPE_NAME,
    typeof(VariantDetailController),
    WebsiteChannelNames = [XperienceConstants.WebsiteChannelName]
)]

namespace KCC.Web.Features.Pages.VariantDetail;

public class VariantDetailController(
    IWebPageDataContextRetriever webPageDataContextRetriever,
    IContentRetriever contentRetriever,
    ITaxonomyRetriever taxonomyRetriever,
    IPreferredLanguageRetriever preferredLanguageRetriever,
    IAuthorNameResolver authorNameResolver,
    IResourceStringInfoProvider resourceStrings,
    IVariantReviewInfoProvider reviewProvider,
    IVariantCookedInfoProvider cookedProvider,
    UserManager<KCCApplicationUser> userManager,
    BreadcrumbService breadcrumbService
) : Controller
{
    public async Task<IActionResult> Index()
    {
        var pageId = webPageDataContextRetriever.Retrieve().WebPage.WebPageItemID;
        var variantPage = await contentRetriever.RetrievePage<RecipeVariant>(pageId, linkedItemsMaxLevel: 1);

        if (variantPage is null)
        {
            return NotFound();
        }

        var parentId = variantPage.SystemFields.WebPageItemParentID;
        var recipePage = await contentRetriever.RetrievePage<Recipe>(parentId);

        if (recipePage is null)
        {
            return NotFound();
        }

        var siblings = await contentRetriever.RetrievePages<RecipeVariant>(
            new(),
            query => query.Where(w => w
                .WhereEquals(nameof(IWebPageFieldsSource.SystemFields.WebPageItemParentID), parentId)
                .WhereNotEquals(nameof(IWebPageFieldsSource.SystemFields.WebPageItemID), pageId)
            ),
            new($"{nameof(VariantDetailController)}|{parentId}|{pageId}|Siblings")
        );

        var language = preferredLanguageRetriever.Get();
        var tagGuids = variantPage.Tags?.Select(t => t.Identifier) ?? [];
        var tagResult = await taxonomyRetriever.RetrieveTags(tagGuids, language);
        var resolvedTags = tagResult?.Select(t => t.Title);

        var variantGuid = variantPage.SystemFields.ContentItemGUID;
        var rating = reviewProvider.GetAverageForVariant(variantGuid);
        var cookedCount = cookedProvider.GetCookedCountForVariant(variantGuid);

        var currentUser = await userManager.GetUserAsync(User);
        var hasCooked = currentUser is not null
            && cookedProvider.HasMemberCooked(variantGuid, currentUser.MemberGuid);

        var viewModel = new VariantDetailViewModel
        {
            VariantName = variantPage.Name,
            VariantDescription = variantPage.Description,
            Icon = variantPage.Icon,
            Images = variantPage.Images,
            PrepTime = variantPage.PrepTime,
            CookTime = variantPage.CookTime,
            Servings = variantPage.ServingNumber,
            Difficulty = variantPage.Difficulty,
            Calories = variantPage.Calories,
            ProteinG = variantPage.ProteinG,
            CarbsG = variantPage.CarbsG,
            FatG = variantPage.FatG,
            FiberG = variantPage.FiberG,
            SugarG = variantPage.SugarG,
            SodiumMg = variantPage.SodiumMg,
            Tags = resolvedTags,
            Ingredients = JsonSerializer.DeserializeCollection<IngredientViewModel>(variantPage.Ingredients),
            Instructions = JsonSerializer.DeserializeCollection<InstructionViewModel>(variantPage.Instructions),
            VariantSlug = variantPage.GetUrl().RelativePath,
            RecipeName = recipePage.Name,
            RecipeSlug = recipePage.GetUrl().RelativePath,
            Breadcrumbs = (await breadcrumbService.BuildBreadcrumbsAsync(pageId))
                .Select(b => new VariantBreadcrumb(b.LinkText, b.Url)),
            SiblingVariants = siblings.Select(s => new SiblingVariantViewModel
            {
                Name = s.Name,
                Slug = s.GetUrl().RelativePath,
                Icon = s.Icon,
            }),
            CreatedByName = await authorNameResolver.Resolve(variantPage.AuthorMemberGuid),
            VariantGuid = variantGuid,
            AverageRating = rating.Average,
            ReviewCount = rating.Count,
            CookedCount = cookedCount,
            HasCooked = hasCooked,
            IsAuthenticated = currentUser is not null,
            ResourceStrings = GetStrings(),
        };

        await variantPage.MapMetadata(viewModel);
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
        "VariantDetail.LoadMore"
    );
}
