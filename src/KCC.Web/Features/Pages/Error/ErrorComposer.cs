using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Extensions;

namespace KCC.Web.Features.Pages.Error;

public class ErrorComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddSingleton<StatusCodePages>();
        builder.SetContentLastChanceFinder<NotFoundContentFinder>();
    }
}
