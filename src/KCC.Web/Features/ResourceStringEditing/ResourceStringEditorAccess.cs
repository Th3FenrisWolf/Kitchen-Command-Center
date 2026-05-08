using Kentico.Content.Web.Mvc;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Web.Mvc;
using Microsoft.AspNetCore.Http;

namespace KCC.Web.Features.ResourceStringEditing;

/// <summary>
/// Reads the current page builder mode for an HTTP context. Exists so that
/// <see cref="ResourceStringEditorAccess"/> can be unit tested without spinning
/// up the Kentico feature registry that backs <c>Kentico().PageBuilder().GetMode()</c>.
/// </summary>
internal interface IPageBuilderModeProvider
{
    PageBuilderMode GetMode(HttpContext httpContext);
}

internal sealed class PageBuilderModeProvider : IPageBuilderModeProvider
{
    public PageBuilderMode GetMode(HttpContext httpContext) =>
        httpContext.Kentico().PageBuilder().GetMode();
}

internal sealed class ResourceStringEditorAccess(
    IHttpContextAccessor httpContextAccessor,
    IPageBuilderModeProvider pageBuilderModeProvider)
    : IResourceStringEditorAccess
{
    public bool CanEdit()
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            return false;
        }

        return pageBuilderModeProvider.GetMode(httpContext) == PageBuilderMode.Edit;
    }
}
