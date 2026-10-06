using KCC.Web.Features.Dictionary;
using KCC.Web.Features.Models.Generated;
using KCC.Web.Features.Pages.Shared;
using KCC.Web.Features.Ramp;
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
    AccountPageQueries accountPages,
    IResourceStringProvider resourceStrings,
    PageMetadata pageMetadata)
    : AsyncRenderController(logger, compositeViewEngine, umbracoContextAccessor)
{
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
        var ramp = Ramps.Of(member);
        RampCookie.Write(HttpContext, ramp);
        var viewModel = new AccountSettingsViewModel
        {
            FirstName = member?.GetValue<string>("firstName"),
            LastName = member?.GetValue<string>("lastName"),
            Email = signedIn.Email,
            Ramp = ramp,
            BackUrl = urls.Account,
            ResourceStrings = resourceStrings.GetGroup("Account"),
        };
        pageMetadata.Apply(page, viewModel);

        return View("~/Features/Pages/Account/Settings/Index.cshtml", viewModel);
    }
}
