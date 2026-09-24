using KCC.Web.Features.Models.Generated;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Core.Services.Navigation;

namespace KCC.Web.Features.Pages.Error;

public interface IStatusCodePages
{
    StatusCodePage Find(int statusCode);
}

// Registered as a singleton because the last-chance content finder is one; both dependencies are singletons too.
public class StatusCodePages(IPublishedContentCache contentCache, IDocumentNavigationQueryService navigation) : IStatusCodePages
{
    public StatusCodePage Find(int statusCode)
    {
        if (!navigation.TryGetRootKeys(out var rootKeys))
        {
            return null;
        }

        foreach (var rootKey in rootKeys)
        {
            if (contentCache.GetById(rootKey) is not ContentFolder folder || !navigation.TryGetChildrenKeys(folder.Key, out var childKeys))
            {
                continue;
            }

            foreach (var childKey in childKeys)
            {
                if (contentCache.GetById(childKey) is StatusCodePage page && page.StatusCode == statusCode)
                {
                    return page;
                }
            }
        }

        return null;
    }
}
