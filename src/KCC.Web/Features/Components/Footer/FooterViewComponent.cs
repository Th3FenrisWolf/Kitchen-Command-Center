using Microsoft.AspNetCore.Mvc;

namespace KCC.Web.Features.Components.Footer;

public class FooterViewComponent : ViewComponent
{
    public IViewComponentResult Invoke()
    {
        return View("~/Features/Components/Footer/Footer.cshtml");
    }
}
