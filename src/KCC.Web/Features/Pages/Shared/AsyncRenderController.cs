using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;

namespace KCC.Web.Features.Pages.Shared;

public abstract class AsyncRenderController(
    ILogger<RenderController> logger,
    ICompositeViewEngine compositeViewEngine,
    IUmbracoContextAccessor umbracoContextAccessor)
    : RenderController(logger, compositeViewEngine, umbracoContextAccessor)
{
    // Route hijacking calls the synchronous Index unless it is hidden like this; the derived async overload serves
    // the page.
    [NonAction]
    public sealed override IActionResult Index() => throw new NotSupportedException();
}
