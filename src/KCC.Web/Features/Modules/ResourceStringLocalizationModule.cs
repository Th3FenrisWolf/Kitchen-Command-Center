using CMS;
using CMS.Core;
using CMS.DataEngine;
using KCC.Web.Features.Modules;
using Kentico.Content.Web.Mvc.Routing;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using ResourceStrings;

[assembly: RegisterModule(typeof(ResourceStringLocalizationModule))]

namespace KCC.Web.Features.Modules;

public class ResourceStringLocalizationModule() : Module(nameof(ResourceStringLocalizationModule))
{
    /// <inheritdoc />
    protected override void OnInit()
    {
        base.OnInit();

        ResourceStringInfoProvider.LanguageRetriever = () =>
            Service.Resolve<IHttpContextAccessor>()
                ?.HttpContext?.RequestServices
                .GetService<IPreferredLanguageRetriever>()
                ?.Get();
    }
}
