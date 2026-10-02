using Umbraco.Cms.Core.Routing;

namespace KCC.Web.Features.Pages.Error;

public class NotFoundContentFinder(StatusCodePages statusCodePages) : IContentLastChanceFinder
{
    public Task<bool> TryFindContent(IPublishedRequestBuilder request)
    {
        var page = statusCodePages.Find(StatusCodes.Status404NotFound);
        if (page is null)
        {
            return Task.FromResult(false);
        }

        // The router has already flagged the request as a 404; supplying content only chooses what renders.
        request.SetPublishedContent(page);
        return Task.FromResult(true);
    }
}
