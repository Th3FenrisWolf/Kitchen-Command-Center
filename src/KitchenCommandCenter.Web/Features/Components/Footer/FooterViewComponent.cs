using Microsoft.AspNetCore.Mvc;

namespace KitchenCommandCenter.Web.Features.Components.Footer;

public class FooterViewComponent : ViewComponent
{
    public IViewComponentResult Invoke()
    {
        return View("~/Features/Components/Footer/Footer.cshtml");
    }
}
