using Kentico.Content.Web.Mvc;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Web.Mvc;
using Microsoft.AspNetCore.Http;

namespace KCC.Web.Features.ResourceStringEditing;

internal interface IPageBuilderModeProvider
{
    PageBuilderMode GetMode(HttpContext httpContext);
}

internal sealed class PageBuilderModeProvider : IPageBuilderModeProvider
{
    public PageBuilderMode GetMode(HttpContext httpContext) =>
        httpContext.Kentico().PageBuilder().GetMode();
}

internal interface IPreviewModeProvider
{
    bool IsPreview(HttpContext httpContext);
}

internal sealed class PreviewModeProvider : IPreviewModeProvider
{
    public bool IsPreview(HttpContext httpContext) =>
        httpContext.Kentico().Preview().Enabled;
}

internal sealed class ResourceStringEditorAccess(
    IHttpContextAccessor httpContextAccessor,
    IPageBuilderModeProvider pageBuilderModeProvider,
    IPreviewModeProvider previewModeProvider)
    : IResourceStringEditorAccess
{
    public bool CanEdit()
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            return false;
        }

        var mode = pageBuilderModeProvider.GetMode(httpContext);
        return mode is PageBuilderMode.Edit or PageBuilderMode.ReadOnly
            || previewModeProvider.IsPreview(httpContext);
    }

    public bool IsPreviewMode()
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            return false;
        }

        var mode = pageBuilderModeProvider.GetMode(httpContext);
        return mode == PageBuilderMode.ReadOnly
            || (previewModeProvider.IsPreview(httpContext) && mode != PageBuilderMode.Edit);
    }
}
