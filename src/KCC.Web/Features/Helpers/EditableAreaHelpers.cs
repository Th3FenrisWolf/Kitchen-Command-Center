using Kentico.PageBuilder.Web.Mvc;

namespace KCC.Web.Features.Helpers;

public static class EditableArea
{
    public static EditableAreaOptions DefaultOptions => new()
    {
        AllowedWidgets = AllowedComponents.ALL,
        AllowWidgetOutputCache = true,
        WidgetOutputCacheExpiresSliding = TimeSpan.FromMinutes(2),
    };

    public static EditableAreaOptions GetAreaOptions(string[] allowedWidgets = null, string defaultSectionIdentifier = null)
    {
        var hasWidgetRestrictions = allowedWidgets?.Length > 0;

        return new()
        {
            AllowedWidgets = hasWidgetRestrictions ? allowedWidgets : AllowedComponents.ALL,
            AllowWidgetOutputCache = true,
            DefaultSectionIdentifier = defaultSectionIdentifier,
            WidgetOutputCacheExpiresSliding = TimeSpan.FromMinutes(2),
        };
    }
}
