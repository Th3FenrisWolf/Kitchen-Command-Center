using CMS.Websites;
using KCC;
using KCC.ResourceStrings.Data;
using KCC.Web.Features.Extensions;
using KCC.Web.Features.Models.Common;
using KCC.Web.Features.Models.Constants;
using KCC.Web.Features.Pages.Account;
using KCC.Web.Features.Pages.Shared;
using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

[assembly: RegisterWebPageRoute(
    AccountPage.CONTENT_TYPE_NAME,
    typeof(AccountController),
    WebsiteChannelNames = [XperienceConstants.WebsiteChannelName]
)]

namespace KCC.Web.Features.Pages.Account;

[Authorize]
public class AccountController(
    UserManager<KCCApplicationUser> userManager,
    IResourceStringInfoProvider resourceStrings,
    IContentRetriever contentRetriever
) : Controller
{
    public async Task<IActionResult> Index()
    {
        if (HttpContext.IsAdmin())
        {
            return StubView();
        }

        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var page = await contentRetriever.RetrieveFirstPage<AccountPage>();
        if (page is null)
        {
            return NotFound();
        }

        var viewModel = new AccountViewModel
        {
            DisplayName = $"{user.FirstName} {user.LastName}".Trim(),
            Initials = AccountViewModel.ComputeInitials(user.FirstName, user.LastName, user.UserName),
            MemberSince = AccountViewModel.FormatMemberSince(user.Created),
            RecipeGroups = await LoadRecipeGroups(user.MemberGuid),
            ResourceStrings = GetStrings(),
        };

        await page.MapMetadata(viewModel);
        return View("~/Features/Pages/Account/Index.cshtml", viewModel);
    }

    /// <summary>
    /// Loads the member's creations: latest versions (so pending submissions appear) plus
    /// published-id sets — the difference marks "Pending review" and published items keep URLs.
    /// </summary>
    /// <param name="memberGuid">The member whose creations to load.</param>
    /// <returns>Ordered display groups for the profile's creations section.</returns>
    private async Task<IEnumerable<RecipeGroupViewModel>> LoadRecipeGroups(Guid memberGuid)
    {
        var myVariants = await contentRetriever.RetrievePages<RecipeVariant>(
            new() { IsForPreview = true },
            query => query.Where(where => where.WhereEquals(nameof(RecipeVariant.AuthorMemberGuid), memberGuid)),
            RetrievalCacheSettings.CacheDisabled
        );

        var myRecipes = await contentRetriever.RetrievePages<Recipe>(
            new() { IsForPreview = true },
            query => query.Where(where => where.WhereEquals(nameof(Recipe.AuthorMemberGuid), memberGuid)),
            RetrievalCacheSettings.CacheDisabled
        );

        var myRecipeIds = myRecipes.Select(recipe => recipe.SystemFields.WebPageItemID).ToHashSet();

        var parentIds = myVariants
            .Select(variant => variant.SystemFields.WebPageItemParentID)
            .Where(parentId => !myRecipeIds.Contains(parentId))
            .Distinct()
            .ToArray();

        IEnumerable<Recipe> parentRecipes = [];
        if (parentIds.Length > 0)
        {
            parentRecipes = await contentRetriever.RetrievePages<Recipe>(
                new() { IsForPreview = true },
                query => query.Where(where => where.WhereIn(nameof(IWebPageFieldsSource.SystemFields.WebPageItemID), parentIds)),
                RetrievalCacheSettings.CacheDisabled
            );
        }

        // Published-set queries stay cached: Kentico's content-change dependencies invalidate them
        // on publish/unpublish. The preview queries above must be uncached to show fresh drafts.
        var publishedVariantIds = (await contentRetriever.RetrievePages<RecipeVariant>(
            new(),
            query => query.Where(where => where.WhereEquals(nameof(RecipeVariant.AuthorMemberGuid), memberGuid)),
            new($"{nameof(AccountController)}|MyVariants|Published|{memberGuid}")
        )).Select(variant => variant.SystemFields.WebPageItemID).ToHashSet();

        var allRecipeIds = myRecipeIds.Concat(parentIds).ToArray();

        var publishedRecipeIds = new HashSet<int>();
        if (allRecipeIds.Length > 0)
        {
            publishedRecipeIds = (await contentRetriever.RetrievePages<Recipe>(
                new(),
                query => query.Where(where => where.WhereIn(nameof(IWebPageFieldsSource.SystemFields.WebPageItemID), allRecipeIds)),
                new($"{nameof(AccountController)}|Recipes|Published|{string.Join(",", allRecipeIds)}")
            )).Select(recipe => recipe.SystemFields.WebPageItemID).ToHashSet();
        }

        var recipeInputs = myRecipes
            .Select(recipe => new AccountViewModel.AuthoredRecipeInput(
                recipe.SystemFields.WebPageItemID, recipe.Name, recipe.Icon, recipe.GetUrl().RelativePath, StartedByMe: true))
            .Concat(parentRecipes.Select(recipe => new AccountViewModel.AuthoredRecipeInput(
                recipe.SystemFields.WebPageItemID, recipe.Name, recipe.Icon, recipe.GetUrl().RelativePath, StartedByMe: false)))
            .ToList();

        var variantInputs = myVariants
            .Select(variant => new AccountViewModel.AuthoredVariantInput(
                variant.SystemFields.WebPageItemID,
                variant.SystemFields.WebPageItemParentID,
                variant.Name,
                variant.Icon,
                variant.GetUrl().RelativePath))
            .ToList();

        return AccountViewModel.BuildRecipeGroups(recipeInputs, variantInputs, publishedRecipeIds, publishedVariantIds);
    }

    private ViewResult StubView() => View("~/Features/Pages/Account/Index.cshtml", new AccountViewModel
    {
        DisplayName = "John Doe",
        Initials = "JD",
        MemberSince = AccountViewModel.FormatMemberSince(new(2025, 3, 5)),
        RecipeGroups =
        [
            new()
            {
                PageId = 1,
                RecipeName = "Mac & Cheese",
                RecipeIcon = "fa-duotone fa-solid fa-pot-food",
                StartedByYou = true,
                Variants =
                [
                    new() { PageId = 10, Name = "Classic Stovetop", Icon = "fa-duotone fa-solid fa-pot-food" },
                    new() { PageId = 11, Name = "Spicy Jalapeño", Icon = "fa-duotone fa-solid fa-pepper-hot", IsPending = true },
                ],
            },
        ],
        ResourceStrings = GetStrings(),
    });

    private Dictionary<string, string> GetStrings() => resourceStrings.GetManyOrDefault(
        "Account.MemberSince",
        "Account.AccountSettings",
        "Account.SignOut",
        "Account.MyRecipesAndVariants",
        "Account.Favorites",
        "Account.RecentActivity",
        "Account.ComingSoon",
        "Account.StartedByYou",
        "Account.PendingReview",
        "Account.NoCreationsYet",
        "Account.RecipesLabel",
        "Account.VariantsLabel"
    );
}
