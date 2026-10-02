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
    AuthoredRecipeQueries authoredRecipes,
    AccountPageQueries accountPages,
    IResourceStringProvider resourceStrings,
    PageMetadata pageMetadata)
    : AsyncRenderController(logger, compositeViewEngine, umbracoContextAccessor)
{
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
            ResourceStrings = resourceStrings.GetGroup("Account"),
        };
        pageMetadata.Apply(page, viewModel);

        return View("~/Features/Pages/Account/Index.cshtml", viewModel);
    }
}
