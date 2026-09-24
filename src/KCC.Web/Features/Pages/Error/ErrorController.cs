using Microsoft.AspNetCore.Mvc;

namespace KCC.Web.Features.Pages.Error;

[Route("error")]
public class ErrorController(IStatusCodePages statusCodePages) : Controller
{
    // No verb attribute: the exception handler re-executes with the failed request's method, POST included.
    [Route("")]
    [Route("{statusCode:int:range(400,599)}")]
    public IActionResult Index(int statusCode = StatusCodes.Status500InternalServerError)
    {
        var page = statusCodePages.Find(statusCode);
        Response.StatusCode = statusCode;

        return View("~/Features/Pages/Error/Index.cshtml", ErrorViewModel.For(statusCode, page?.Heading, page?.Body?.ToHtmlString()));
    }
}
