using System.Globalization;
using AutoMapper;
using Kentico.Content.Web.Mvc;
using Microsoft.AspNetCore.Mvc;

namespace KCC.Web.Features.Pages.Error;

[Route("Error")]
public class ErrorController(
    IContentRetriever contentRetriever,
    IMapper mapper
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

        var viewModel = mapper.Map<ErrorViewModel>(page);

        return View("~/Features/Pages/Error/Index.cshtml", viewModel);
    }
}
