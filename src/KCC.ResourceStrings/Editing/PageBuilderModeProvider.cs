using Kentico.Content.Web.Mvc;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Web.Mvc;
using Microsoft.AspNetCore.Http;

namespace KCC.ResourceStrings.Editing;

public interface IPageBuilderModeProvider
{
    PageBuilderMode GetMode(HttpContext httpContext);
}

public sealed class PageBuilderModeProvider : IPageBuilderModeProvider
{
    public PageBuilderMode GetMode(HttpContext httpContext) =>
        httpContext.Kentico().PageBuilder().GetMode();
}
