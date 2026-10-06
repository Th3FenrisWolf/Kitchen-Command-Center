using KCC.Web.Features.Dictionary;
using Microsoft.AspNetCore.Mvc;

namespace KCC.Web.Features.Components.Header;

public class HeaderViewComponent(IResourceStringProvider resourceStrings, NavModelBuilder navModels) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync() => View(
        "~/Features/Components/Header/Header.cshtml",
        new HeaderViewModel
        {
            LogoAlt = resourceStrings.GetOrDefault("Shared.LogoAlt"),
            Nav = await navModels.BuildAsync(HttpContext),
        });
}
