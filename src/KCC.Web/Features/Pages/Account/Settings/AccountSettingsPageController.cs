using KCC.Web.Features.Dictionary;
using KCC.Web.Features.Models.Generated;
using KCC.Web.Features.Pages.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;

namespace KCC.Web.Features.Pages.Account.Settings;

public class AccountSettingsPageController(
    ILogger<RenderController> logger,
    ICompositeViewEngine compositeViewEngine,
    IUmbracoContextAccessor umbracoContextAccessor,
    IMemberManager memberManager,
    IMemberService memberService,
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
        if (CurrentPage is not AccountSettingsPage page)
        {
            return NotFound();
        }

        var urls = accountPages.GetUrls();
        if (await memberManager.GetCurrentMemberAsync() is not { } signedIn)
        {
            return SignInRedirect.To(urls.Login, Request);
        }

        var member = memberService.GetById(signedIn.Key);
        var viewModel = new AccountSettingsViewModel
        {
            FirstName = member?.GetValue<string>("firstName"),
            LastName = member?.GetValue<string>("lastName"),
            Email = signedIn.Email,
            BackUrl = urls.Account,
            ResourceStrings = GetStrings(),
        };
        pageMetadata.Apply(page, viewModel);

        return View("~/Features/Pages/Account/Settings/Index.cshtml", viewModel);
    }

    private Dictionary<string, string> GetStrings() => resourceStrings.GetManyOrDefault(
        "Account.BackToProfile",
        "Account.AccountSettings",
        "Account.Profile",
        "Account.FirstName",
        "Account.LastName",
        "Account.Email",
        "Account.EmailComingSoon",
        "Account.EmailComingSoonNote",
        "Account.SaveChanges",
        "Account.ChangePassword",
        "Account.CurrentPassword",
        "Account.NewPassword",
        "Account.ConfirmNewPassword",
        "Account.UpdatePassword",
        "Account.SignOut",
        "Account.PasswordsDoNotMatch",
        "Account.ProfileSaved",
        "Account.PasswordUpdated",
        "Account.UnexpectedError");
}
