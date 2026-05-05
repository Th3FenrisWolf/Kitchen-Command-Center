using CMS.Core;
using CMS.Websites;
using KCC.Admin.Models.Retrievers;
using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using Microsoft.AspNetCore.Mvc;

namespace KCC.Web.Features.Extensions;

public static class UrlHelperExtensions
{
    public static string HomePage(this IUrlHelper _)
    {
        var contentRetriever = Service.Resolve<IContentRetriever>();

        var page = contentRetriever.RetrievePages<HomePage>(
            new(),
            query => query.TopN(1),
            new($"{nameof(UrlHelperExtensions)}|{nameof(HomePage)}")
        ).GetAwaiter().GetResult().FirstOrDefault();

        return page.GetUrl().RelativePath;
    }

    public static string LocalizedAction(this IUrlHelper urlHelper, string action, string controller)
    {
        var lang = Service.Resolve<IPreferredLanguageRetriever>().Get();

        var path = urlHelper.Action(action, controller);

        if (string.IsNullOrEmpty(lang) || lang == DefaultLanguageRetriever.GetName())
        {
            return path.ToLowerInvariant();
        }

        return $"/{lang}{path}".ToLowerInvariant();
    }
}
