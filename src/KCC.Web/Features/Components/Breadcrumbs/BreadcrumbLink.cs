namespace KCC.Web.Features.Components.Breadcrumbs;

public record BreadcrumbLink
(
    string LinkText,
    string Url,
    int? ParentId = null,
    int? WebPageItemId = null
);
