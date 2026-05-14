using Kentico.Content.Web.Mvc;
using Kentico.Web.Mvc;
using Microsoft.AspNetCore.Http;

namespace KCC.ResourceStrings.Editing;

public interface IPreviewModeProvider
{
    bool IsPreview(HttpContext httpContext);
}

public sealed class PreviewModeProvider : IPreviewModeProvider
{
    public bool IsPreview(HttpContext httpContext) =>
        httpContext.Kentico().Preview().Enabled;
}
