using System.Globalization;
using CMS.Websites;
using KCC.Web.Features.Pages.Shared;
using Kentico.Content.Web.Mvc;
using Microsoft.AspNetCore.Mvc;

namespace KCC.Web.Features.Components.Breadcrumbs;

public class BreadcrumbsViewComponent(
    IContentRetriever contentRetriever
) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(BasePageViewModel sourceViewModel)
    {
        if (!sourceViewModel.ShowBreadcrumbs)
        {
            return Content(string.Empty);
        }

        var currentPage = await GetCurrentPage(sourceViewModel.WebPageItemID);

        var parentId = currentPage?.SystemFields?.WebPageItemParentID;

        if (parentId == null)
        {
            return Content(string.Empty);
        }

        var nodes = new List<BreadcrumbLink> { CreateBreadcrumbLink(currentPage, false) };

        while (parentId != 0)
        {
            var nextNode = await GetParent(parentId.Value);

            nodes.Add(nextNode);

            parentId = nextNode.ParentId;
        }

        nodes.Add(new BreadcrumbLink { LinkText = "Home", Url = "/" });

        nodes.Reverse();

        var viewModel = new BreadcrumbsViewModel { Links = nodes };

        return View("~/Features/Components/Breadcrumbs/Breadcrumbs.cshtml", viewModel);
    }

    private static BreadcrumbLink CreateBreadcrumbLink(
        IWebPageFieldsSource node,
        bool isNavigable = true
    )
    {
        return node is INavigation_Metadata navNode
            ? new BreadcrumbLink
            {
                LinkText = !string.IsNullOrEmpty(navNode.NavigationLabel)
                    ? navNode.NavigationLabel
                    : navNode.MetadataTitle,
                Url = isNavigable ? $"/{node.SystemFields.WebPageUrlPath}" : string.Empty,
                ParentId = node.SystemFields.WebPageItemParentID,
            }
            : null;
    }

    // TODO: Refactor the two methods below here, since they mostly use the same code.
    // Easily should be able to pull out the query and data retrieval and use it in both
    private async Task<IWebPageFieldsSource> GetCurrentPage(int pageId)
    {
        var pages = await contentRetriever.RetrieveAllPages<IWebPageFieldsSource>(
            new(),
            query => query
                .Where(where => where
                    .WhereEquals(nameof(IWebPageFieldsSource.SystemFields.WebPageItemID), pageId)
                )
                .TopN(1)
                .Columns(
                    nameof(IWebPageFieldsSource.SystemFields.WebPageItemID),
                    nameof(IWebPageFieldsSource.SystemFields.WebPageItemParentID),
                    nameof(IWebPageFieldsSource.SystemFields.WebPageUrlPath),
                    nameof(INavigation_Metadata.NavigationLabel),
                    nameof(INavigation_Metadata.MetadataTitle)
                ),
            new($"{nameof(BreadcrumbsViewComponent)}|{nameof(InvokeAsync)}|{pageId}")
        );

        return pages.FirstOrDefault();
    }

    // TODO: Figure out a solution that works with folders. Currently, this breaks due to folders not
    // being able to be resolved from the same method as web page items
    private async Task<BreadcrumbLink> GetParent(int parentId)
    {
        var pages = await contentRetriever.RetrieveAllPages<IWebPageFieldsSource>(
            new(),
            query => query
                .Where(where => where
                    .WhereEquals(nameof(IWebPageFieldsSource.SystemFields.WebPageItemID), parentId)
                )
                .TopN(1)
                .Columns(
                    nameof(IWebPageFieldsSource.SystemFields.WebPageItemID),
                    nameof(IWebPageFieldsSource.SystemFields.WebPageItemParentID),
                    nameof(IWebPageFieldsSource.SystemFields.WebPageUrlPath),
                    nameof(INavigation_Metadata.NavigationLabel),
                    nameof(INavigation_Metadata.MetadataTitle)
                ),
            new($"{nameof(BreadcrumbsViewComponent)}|{nameof(GetParent)}|{parentId}")
        );

        return CreateBreadcrumbLink(pages.FirstOrDefault());
    }
}
