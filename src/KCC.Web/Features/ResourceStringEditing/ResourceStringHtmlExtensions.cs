using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.DependencyInjection;
using ResourceStrings;

namespace KCC.Web.Features.ResourceStringEditing;

public static class ResourceStringHtmlExtensions
{
    public static IHtmlContent ResourceString(this IHtmlHelper html, string key)
    {
        var services = html.ViewContext.HttpContext.RequestServices;
        var provider = services.GetRequiredService<IResourceStringInfoProvider>();
        var access = services.GetRequiredService<IResourceStringEditorAccess>();

        var value = provider.GetOrDefault(key);
        return BuildContent(key, value, access.CanEdit());
    }

    internal static IHtmlContent BuildContent(string key, string value, bool canEdit)
    {
        var encoder = HtmlEncoder.Default;
        var encodedValue = encoder.Encode(value ?? string.Empty);

        if (!canEdit)
        {
            return new HtmlString(encodedValue);
        }

        var encodedKey = encoder.Encode(key);
        return new HtmlString(
            $"<span class=\"kcc-rs-editable\" data-resource-key=\"{encodedKey}\">{encodedValue}</span>");
    }
}
