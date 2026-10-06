using KCC.Web.Features.Dictionary;
using KCC.Web.Features.Models.Generated;
using KCC.Web.Features.Pages.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;

namespace KCC.Web.Features.Pages.Account.Login;

public class LoginPageController(
    ILogger<RenderController> logger,
    ICompositeViewEngine compositeViewEngine,
    IUmbracoContextAccessor umbracoContextAccessor,
    IMemberManager memberManager,
    IResourceStringProvider resourceStrings,
    PageMetadata pageMetadata)
    : AsyncRenderController(logger, compositeViewEngine, umbracoContextAccessor)
{
    public const string RegisterMode = "register";

    public async Task<IActionResult> Index([FromQuery] string returnUrl, [FromQuery] string mode, CancellationToken cancellationToken)
    {
        if (CurrentPage is not LoginPage page)
        {
            return NotFound();
        }

        var localReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : null;
        if (await memberManager.GetCurrentMemberAsync() is not null)
        {
            return Redirect(localReturnUrl ?? "/");
        }

        var viewModel = new LoginViewModel
        {
            ReturnUrl = localReturnUrl,
            Register = string.Equals(mode, RegisterMode, StringComparison.OrdinalIgnoreCase),
            ResourceStrings = resourceStrings.GetGroup("Login"),
        };
        pageMetadata.Apply(page, viewModel);

        return View("~/Features/Pages/Account/Login/Index.cshtml", viewModel);
    }
}
