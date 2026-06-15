using CMS.Websites;
using KCC;
using KCC.ResourceStrings.Data;
using KCC.Web.Features.Extensions;
using KCC.Web.Features.Models.Common;
using KCC.Web.Features.Models.Constants;
using KCC.Web.Features.Pages.Account.Settings;
using KCC.Web.Features.Pages.Shared;
using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

[assembly: RegisterWebPageRoute(
    AccountSettingsPage.CONTENT_TYPE_NAME,
    typeof(AccountSettingsController),
    WebsiteChannelNames = [XperienceConstants.WebsiteChannelName]
)]

namespace KCC.Web.Features.Pages.Account.Settings;

[Authorize]
public class AccountSettingsController(
    UserManager<KCCApplicationUser> userManager,
    IContentRetriever contentRetriever,
    IResourceStringInfoProvider resourceStrings
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

        var accountPage = await contentRetriever.RetrieveFirstPage<AccountPage>();

        if (accountPage is null)
        {
            return NotFound();
        }

        var viewModel = new AccountSettingsViewModel
        {
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            BackUrl = accountPage?.GetUrl().RelativePath,
            ResourceStrings = GetStrings(),
        };

        await accountPage.MapMetadata(viewModel);
        return View("~/Features/Pages/Account/Settings/Index.cshtml", viewModel);
    }

    private ViewResult StubView() => View(
        "~/Features/Pages/Account/Settings/Index.cshtml",
        new AccountSettingsViewModel
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            BackUrl = "/account",
            ResourceStrings = GetStrings(),
        });

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
        "Account.UnexpectedError"
    );
}
