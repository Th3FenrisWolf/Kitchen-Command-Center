namespace KCC.Web.Features.Components.Breadcrumbs;

public class BreadcrumbsViewModel
{
    public IEnumerable<BreadcrumbLink> Links { get; set; }
}

// TODO: Move this out to another file
public class BreadcrumbLink
{
    public string LinkText { get; set; }
    public string Url { get; set; }
    public int ParentId { get; set; }
}
