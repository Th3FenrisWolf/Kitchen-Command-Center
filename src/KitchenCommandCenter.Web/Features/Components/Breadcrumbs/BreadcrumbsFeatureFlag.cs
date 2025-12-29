using BizStream.XbK.FeatureFlags;

namespace KitchenCommandCenter.Web.Features.Components.Breadcrumbs;

public record BreadcrumbsFeatureFlag : FeatureFlag
{
    public override string CodeName => nameof(BreadcrumbsFeatureFlag);
    public override string DisplayName => "Breadcrumb Component";
    public override bool Enabled { get; init; } = true;
}
