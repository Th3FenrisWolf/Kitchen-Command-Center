using System;
using System.Collections.Generic;
using System.Linq;
using Kentico.PageBuilder.Web.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KitchenCommandCenter.Web.Extensions;

public static class HtmlHelperExtensions
{
    public static EditableAreaOptions GetAreaOptions(this IHtmlHelper _, string[] allowedWidgets = null, string defaultSectionIdentifier = null)
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

    public static string Clsx(this IHtmlHelper htmlHelper, params object[] classNames)
    {
        var classList = classNames.SelectMany(cName => cName switch
        {
            string str when !string.IsNullOrWhiteSpace(str) => [str],
            IEnumerable<object> collection => collection.SelectMany(item => htmlHelper.Clsx(item).Split(' ', StringSplitOptions.RemoveEmptyEntries)),
            (bool condition, object value) => condition ? [value.ToString()] : [],
            _ => []
        });

        return string.Join(" ", classList.Distinct());
    }
}
