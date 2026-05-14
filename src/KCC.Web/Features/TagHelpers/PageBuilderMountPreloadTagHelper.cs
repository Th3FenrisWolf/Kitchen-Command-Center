using KCC.ResourceStrings.Editing;
using Kentico.PageBuilder.Web.Mvc;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Vite.AspNetCore;

namespace KCC.Web.Features.TagHelpers;

public sealed class PageBuilderMountPreloadTagHelper(
    IHttpContextAccessor httpContextAccessor,
    IPageBuilderModeProvider modeProvider,
    IViteManifest manifest,
    IViteDevServerStatus devServer)
    : TagHelper
{
    private const string EntryKey = "Features/PageBuilderMount.ts";

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

        if (devServer.IsEnabled)
        {
            var basePath = string.IsNullOrEmpty(devServer.BasePath)
                ? string.Empty
                : devServer.BasePath.TrimEnd('/');
            output.Attributes.SetAttribute("href", $"{basePath}/{EntryKey}");
        }
        else
        {
            var chunk = manifest[EntryKey]
                ?? throw new InvalidOperationException(
                    $"Vite manifest is missing entry '{EntryKey}'.");
            output.Attributes.SetAttribute("href", $"/{chunk.File}");
        }
    }
}
