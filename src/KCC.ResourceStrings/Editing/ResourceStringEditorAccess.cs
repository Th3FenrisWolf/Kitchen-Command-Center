using Kentico.Membership;
using Kentico.PageBuilder.Web.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace KCC.ResourceStrings.Editing;

public interface IResourceStringEditorAccess
{
    bool CanEdit();
    bool IsPreviewMode();
}

internal sealed class ResourceStringEditorAccess(
    IHttpContextAccessor httpContextAccessor,
    IPageBuilderModeProvider pageBuilderModeProvider,
    IPreviewModeProvider previewModeProvider)
    : IResourceStringEditorAccess
{
    private const string CanEditCacheKey = "KCC.ResourceStrings.CanEdit";

    public bool CanEdit()
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            return false;
        }

        if (httpContext.Items.TryGetValue(CanEditCacheKey, out var cached) && cached is bool cachedBool)
        {
            return cachedBool;
        }

        var result = ComputeCanEdit(httpContext);
        httpContext.Items[CanEditCacheKey] = result;
        return result;
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

    private bool ComputeCanEdit(HttpContext httpContext)
    {
        var mode = pageBuilderModeProvider.GetMode(httpContext);

        // Page Builder Edit / ReadOnly modes are only reachable from inside the Xperience
        // admin UI; Kentico already gates them on admin auth.
        if (mode is PageBuilderMode.Edit or PageBuilderMode.ReadOnly)
        {
            return true;
        }

        // Preview mode covers two cases: an authenticated admin opening a preview link,
        // and a third party following a shareable preview URL (no admin cookie).
        // Only the first should be able to edit resource strings.
        if (previewModeProvider.IsPreview(httpContext))
        {
            return IsAuthenticatedAsAdmin(httpContext);
        }

        return false;
    }

    private static bool IsAuthenticatedAsAdmin(HttpContext httpContext) =>
        SchemeIsAuthenticated(httpContext, AdminIdentityConstants.APPLICATION_SCHEME)
        || SchemeIsAuthenticated(httpContext, AdminIdentityConstants.EXTERNAL_SCHEME);

    private static bool SchemeIsAuthenticated(HttpContext httpContext, string scheme)
    {
        // Sync wait is acceptable: cookie auth is a pure-CPU cookie decrypt, and the
        // result is cached per request via CanEdit's HttpContext.Items cache.
        var result = httpContext.AuthenticateAsync(scheme).GetAwaiter().GetResult();
        return result.Succeeded;
    }
}
