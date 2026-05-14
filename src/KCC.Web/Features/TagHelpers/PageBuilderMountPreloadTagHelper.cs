using KCC.Web.Features.ResourceStringEditing;
using Kentico.PageBuilder.Web.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace KCC.Web.Features.TagHelpers;

internal sealed class PageBuilderMountPreloadTagHelper(
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

        output.TagName = "link";
        output.TagMode = TagMode.SelfClosing;
        output.Attributes.SetAttribute("rel", "modulepreload");
        output.Attributes.SetAttribute("vite-href", "/Features/PageBuilderMount.ts");
        output.Attributes.SetAttribute("asp-append-version", "true");
    }
}
