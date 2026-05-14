using Microsoft.AspNetCore.Mvc.Rendering;

namespace KCC.Web.Features.Helpers;

public static class Html
{
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