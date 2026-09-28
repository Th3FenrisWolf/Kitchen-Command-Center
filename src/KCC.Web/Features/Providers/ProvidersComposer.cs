using Anthropic;
using Anthropic.Core;
using KCC.Admin;
using KCC.Web.Features.Models.Options;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Extensions;

namespace KCC.Web.Features.Providers;

public class ProvidersComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddSingleton<IAuthorNameProvider, AuthorNameProvider>();

        var anthropic = builder.Config.GetSection(AnthropicOptions.SectionName).Get<AnthropicOptions>() ?? new();
        builder.Services.AddSingleton(anthropic);
        builder.Services.AddSingleton(new AnthropicClient(new ClientOptions { ApiKey = anthropic.ApiKey ?? string.Empty }));
        builder.Services.AddSingleton<IRecipeIconService, RecipeIconProvider>();
        builder
            .AddNotificationHandler<MemberSavedNotification, AuthorNameCacheRefresher>()
            .AddNotificationHandler<MemberDeletedNotification, AuthorNameCacheRefresher>();
    }
}
