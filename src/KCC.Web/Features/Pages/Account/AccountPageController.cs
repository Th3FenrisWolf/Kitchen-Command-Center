using KCC.Web.Features.Dictionary;
using KCC.Web.Features.Models.Generated;
using KCC.Web.Features.Pages.Shared;
using KCC.Web.Features.Providers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;

namespace KCC.Web.Features.Pages.Account;

public class AccountPageController(
    ILogger<RenderController> logger,
    ICompositeViewEngine compositeViewEngine,
    IUmbracoContextAccessor umbracoContextAccessor,
    IMemberManager memberManager,
    IMemberService memberService,
    IAuthoredRecipeQueries authoredRecipes,
    IAccountPageQueries accountPages,
    IResourceStringProvider resourceStrings,
    PageMetadata pageMetadata)
    : RenderController(logger, compositeViewEngine, umbracoContextAccessor)
{
    // Route hijacking calls the synchronous Index unless it is hidden like this; the overload below serves the page.
    [NonAction]
    public sealed override IActionResult Index() => throw new NotSupportedException();

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (CurrentPage is not AccountPage page)
        {
            return NotFound();
        }

        var urls = accountPages.GetUrls();
        if (await memberManager.GetCurrentMemberAsync() is not { } signedIn)
        {
            return SignInRedirect.To(urls.Login, Request);
        }

        var member = memberService.GetById(signedIn.Key);
        var firstName = member?.GetValue<string>("firstName");
        var lastName = member?.GetValue<string>("lastName");
        var authored = authoredRecipes.GetAuthoredBy(signedIn.Key);
        var viewModel = new AccountViewModel
        {
            DisplayName = AuthorNameProvider.FormatDisplayName(firstName, lastName, signedIn.UserName),
            Initials = AccountViewModel.ComputeInitials(firstName, lastName, signedIn.UserName),
            MemberSince = AccountViewModel.FormatMemberSince(member?.CreateDate),
            SettingsUrl = urls.Settings,
            RecipeGroups = AccountViewModel.BuildRecipeGroups(
                authored.Recipes,
                authored.Variants,
                authored.PublishedRecipeKeys,
                authored.PublishedVariantKeys).ToList(),
            ResourceStrings = GetStrings(),
        };
        pageMetadata.Apply(page, viewModel);

        return View("~/Features/Pages/Account/Index.cshtml", viewModel);
    }

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
        "Account.VariantsLabel");
}
