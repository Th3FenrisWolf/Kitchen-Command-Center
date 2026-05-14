using CMS.ContentEngine;
using CMS.Websites;
using CMS.Websites.Routing;
using KCC.Web.Features.Extensions;

namespace KCC.Web.Features.Components.Breadcrumbs;

public class BreadcrumbService(
    IContentQueryExecutor queryExecutor,
    IWebsiteChannelContext websiteChannelContext,
    IWebPageManagerFactory webPageManagerFactory
)
{
    private const int RootParentId = 0;
    private const int SystemUserId = 0;
    private const string HomePageTitle = "Home";
    private const string HomePageUrl = "/";

    private readonly Lazy<IWebPageManager> webPageManager = new(() => webPageManagerFactory.Create(
        websiteChannelContext.WebsiteChannelID, userId: SystemUserId
    ));

    public async Task<List<BreadcrumbLink>> BuildBreadcrumbsAsync(int webPageItemId)
    {
        var items = new List<BreadcrumbLink>();

        var currentPage = await GetWebPage(webPageItemId);
        if (currentPage == null)
        {
            return items;
        }

        var parentId = currentPage.SystemFields.WebPageItemParentID;

        items.Add(await CreateBreadcrumbLink(currentPage, isCurrentPage: true));

        while (parentId != RootParentId)
        {
            var parent = await GetWebPage(parentId);
            if (parent != null)
            {
                items.Add(await CreateBreadcrumbLink(parent, isCurrentPage: false));
                parentId = parent.SystemFields.WebPageItemParentID;
                continue;
            }

            // Page not found - check if it's a folder and get its parent ID to continue traversing
            var folderMetadata = await webPageManager.Value.GetWebPageMetadata(parentId);
            if (folderMetadata == null)
            {
                break;
            }

            // Skip folders but continue traversing using their parent ID
            parentId = folderMetadata.ParentID;
        }

        items.Add(new(LinkText: HomePageTitle, Url: HomePageUrl));

        items.Reverse();
        return items;
    }

    private static async Task<BreadcrumbLink> CreateBreadcrumbLink(IWebPageFieldsSource page, bool isCurrentPage) =>
        new
        (
            LinkText: await page.GetBreadcrumbTitle(),
            Url: isCurrentPage ? string.Empty : page.GetUrl().RelativePath,
            ParentId: page.SystemFields.WebPageItemParentID,
            WebPageItemId: page.SystemFields.WebPageItemID
        );

    private async Task<IWebPageFieldsSource> GetWebPage(int id)
    {
        var builder = new ContentItemQueryBuilder()
            .ForContentTypes(query => query
                .OfReusableSchema(IMetadata.REUSABLE_FIELD_SCHEMA_NAME)
                .ForWebsite(websiteChannelContext.WebsiteChannelName))
            .Parameters(p => p
                .Where(w => w.WhereEquals(nameof(WebPageFields.WebPageItemID), id))
                .TopN(1)
            );

        var options = new ContentQueryExecutionOptions
        {
            ForPreview = websiteChannelContext.IsPreview
        };

        var results = await queryExecutor.GetMappedWebPageResult<IWebPageFieldsSource>(builder, options);
        return results.FirstOrDefault();
    }
}
