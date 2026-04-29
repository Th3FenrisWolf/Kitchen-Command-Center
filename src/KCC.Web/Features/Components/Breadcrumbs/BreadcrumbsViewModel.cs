namespace KCC.Web.Features.Components.Breadcrumbs;

public class BreadcrumbsViewModel
{
    public IEnumerable<BreadcrumbLink> Links { get; set; }
}

public record BreadcrumbLink
(
    string LinkText,
    string Url,
    int? ParentId = null,
    int? WebPageItemId = null
);
