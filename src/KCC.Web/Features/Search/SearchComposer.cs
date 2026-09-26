using KCC.Contributions;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Extensions;

namespace KCC.Web.Features.Search;

public class SearchComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.Configure<RecipeSearchOptions>(builder.Config.GetSection("RecipeSearch"));
        builder.Services.AddSingleton<RecipeIndex>();
        builder.Services.AddSingleton<IRecipeSearchService, RecipeSearchService>();
        builder.Services.AddScoped<IRecipeIndexSource, RecipeIndexSource>();
        builder.Services.AddSingleton<RecipeIndexRebuilder>();
        builder.Services.AddSingleton<IRecipeIndexRebuilder>(services => services.GetRequiredService<RecipeIndexRebuilder>());
        builder.Services.AddHostedService(services => services.GetRequiredService<RecipeIndexRebuilder>());
        builder
            .AddNotificationHandler<ContentCacheRefresherNotification, RecipeIndexTriggers>()
            .AddNotificationHandler<MemberSavedNotification, RecipeIndexTriggers>()
            .AddNotificationHandler<MemberDeletedNotification, RecipeIndexTriggers>()
            .AddNotificationHandler<ReviewsChangedNotification, RecipeIndexTriggers>();
    }
}
