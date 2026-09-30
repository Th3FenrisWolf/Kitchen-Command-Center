using KCC.Web.Features.Dictionary;
using KCC.Web.Features.Models.Generated;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Extensions;

namespace KCC.Web.Features.Components.Breadcrumbs;

public class BreadcrumbService(IResourceStringProvider resourceStrings)
{
    public IReadOnlyList<BreadcrumbLink> Build(IPublishedContent page) => BreadcrumbTrail.Build(
        resourceStrings.GetOrDefault("Shared.Home"),
        page.AncestorsOrSelf().Reverse().Select(node => new BreadcrumbTrail.Crumb(Label(node), node.Url())).ToList());

    private static string Label(IPublishedContent node) => node is IMetadata metadata
        ? BreadcrumbTrail.Label(metadata.BreadcrumbLabel, metadata.MetadataTitle, node.Name)
        : node.Name;
}
