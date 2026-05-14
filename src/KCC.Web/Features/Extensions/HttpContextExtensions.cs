using Kentico.Content.Web.Mvc;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Web.Mvc;

namespace KCC.Web.Features.Extensions;

public static class HttpContextExtensions
{
    public static bool IsPreview(this HttpContext context) => context.Kentico().Preview().Enabled;

    public static bool IsPageBuilder(this HttpContext context) =>
        context.Kentico().PageBuilder().GetMode() is PageBuilderMode.ReadOnly or PageBuilderMode.Edit;
}
