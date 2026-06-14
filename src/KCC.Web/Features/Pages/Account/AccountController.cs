using KCC;
using KCC.ResourceStrings.Data;
using KCC.Web.Features.Extensions;
using KCC.Web.Features.Models.Common;
using KCC.Web.Features.Models.Constants;
using KCC.Web.Features.Pages.Account;
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

        var viewModel = new AccountViewModel
        {
            DisplayName = $"{user.FirstName} {user.LastName}".Trim(),
            Initials = AccountViewModel.ComputeInitials(user.FirstName, user.LastName, user.UserName),
            MemberSince = AccountViewModel.FormatMemberSince(user.Created),
            ResourceStrings = GetStrings(),
        };

        return View("~/Features/Pages/Account/Index.cshtml", viewModel);
    }

    private ViewResult StubView() => View("~/Features/Pages/Account/Index.cshtml", new AccountViewModel
    {
        DisplayName = "John Doe",
        Initials = "JD",
        MemberSince = AccountViewModel.FormatMemberSince(new(2025, 3, 5)),
        ResourceStrings = GetStrings(),
    });

    private Dictionary<string, string> GetStrings() => resourceStrings.GetManyOrDefault(
        "Account.MemberSince",
        "Account.AccountSettings",
        "Account.SignOut",
        "Account.MyRecipesAndVariants",
        "Account.Favorites",
        "Account.RecentActivity",
        "Account.ComingSoon"
    );
}
