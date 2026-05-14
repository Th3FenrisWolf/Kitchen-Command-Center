using KCC.Web.Features.ResourceStringEditing;
using Kentico.PageBuilder.Web.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace KCC.Web.Features.TagHelpers;

internal sealed class PageBuilderMountScriptTagHelper(
    IHttpContextAccessor httpContextAccessor,
    IPageBuilderModeProvider modeProvider)
    : TagHelper
{
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            output.SuppressOutput();
            return;
        }

        var mode = modeProvider.GetMode(httpContext);
        if (mode != PageBuilderMode.Edit && mode != PageBuilderMode.ReadOnly)
        {
            output.SuppressOutput();
            return;
        }

        output.TagName = "script";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.SetAttribute("type", "module");
        output.Attributes.SetAttribute("vite-src", "/Features/PageBuilderMount.ts");
        output.Attributes.SetAttribute("asp-append-version", "true");
    }
}
