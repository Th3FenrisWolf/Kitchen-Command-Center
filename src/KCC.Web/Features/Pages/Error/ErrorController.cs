using System.Globalization;
using KCC.Web.Features.Attributes;
using Kentico.Content.Web.Mvc;
using Microsoft.AspNetCore.Mvc;

namespace KCC.Web.Features.Pages.Error;

[LocalizedRoute("Error")]
public class ErrorController(
    IContentRetriever contentRetriever
) : Controller
{
    public async Task<IActionResult> Index()
    {
        return await HandleStatusCode(
            HttpContext.Response.StatusCode.ToString(CultureInfo.InvariantCulture)
        );
    }

    [Route("{statusCode}")]
    public async Task<IActionResult> HandleStatusCode(string statusCode)
    {
        var page = (await contentRetriever.RetrievePages<StatusCodePage>(
            new(),
            query => query
                .Where(where => where
                    .WhereEquals(nameof(StatusCodePage.StatusCode), statusCode)
                )
                .TopN(1),
            new($"{nameof(ErrorController)}|{nameof(HandleStatusCode)}|{statusCode}")
        )).FirstOrDefault();

        var viewModel = new ErrorViewModel
        {
            Heading = page.StatusCodeHeading,
            Body = page.StatusCodeBody,
            Title = page.StatusCodeHeading,
        };

        return View("~/Features/Pages/Error/Index.cshtml", viewModel);
    }
}
