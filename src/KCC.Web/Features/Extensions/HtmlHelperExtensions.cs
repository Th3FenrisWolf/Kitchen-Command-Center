using Kentico.Content.Web.Mvc;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Web.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KCC.Web.Features.Extensions;

public static class HtmlHelperExtensions
{
    public static bool IsPreview(this IHtmlHelper html) =>
        html.ViewContext.HttpContext.Kentico().Preview().Enabled;

    public static bool IsPageBuilder(this IHtmlHelper html) =>
        html.ViewContext.HttpContext.Kentico().PageBuilder().GetMode()
            is PageBuilderMode.ReadOnly or PageBuilderMode.Edit;

    public static string Clsx(this IHtmlHelper html, params object[] classNames)
    {
        var classList = classNames.SelectMany(cName => cName switch
        {
            string str when !string.IsNullOrWhiteSpace(str) => [str],
            IEnumerable<object> collection => collection.SelectMany(item => html.Clsx(item).Split(' ', StringSplitOptions.RemoveEmptyEntries)),
            (bool condition, object value) => condition ? [value.ToString()] : [],
            _ => []
        });

        return string.Join(" ", classList.Distinct());
    }
}
