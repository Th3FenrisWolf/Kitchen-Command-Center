using KCC.Web.Features.Dictionary;
using Microsoft.AspNetCore.Mvc;

namespace KCC.Web.Features.Components.Header;

public class HeaderViewComponent(SiteSettingsQueries siteSettings, IResourceStringProvider resourceStrings) : ViewComponent
{
    public IViewComponentResult Invoke()
    {
        var navigation = siteSettings.GetHeaderNavigation();
        var isSignedIn = User.Identity?.IsAuthenticated == true;

        var viewModel = new HeaderViewModel
        {
            LogoAlt = resourceStrings.GetOrDefault("Shared.LogoAlt"),
            SwitchToLightLabel = resourceStrings.GetOrDefault("Theme.SwitchToLight"),
            SwitchToDarkLabel = resourceStrings.GetOrDefault("Theme.SwitchToDark"),
            MainNavItems = HeaderNav.Visible(navigation.Main, isSignedIn).ToList(),
            UtilityNavItems = HeaderNav.Visible(navigation.Utility, isSignedIn).ToList(),
        };

        return View("~/Features/Components/Header/Header.cshtml", viewModel);
    }
}
