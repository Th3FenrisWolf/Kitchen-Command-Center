using System.Globalization;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;

namespace KCC.Web.Features.Recipes;

public class TaxonomyDefaultsOnStartup(
    IServiceScopeFactory scopeFactory,
    IKeyValueService keyValues,
    IRuntimeState runtimeState,
    ILogger<TaxonomyDefaultsOnStartup> logger) : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    public const string AppliedKey = "KCC.TaxonomyDefaults";

    public async Task HandleAsync(UmbracoApplicationStartedNotification notification, CancellationToken cancellationToken)
    {
        if (runtimeState.Level != RuntimeLevel.Run || keyValues.GetValue(AppliedKey) is not null)
        {
            return;
        }

        try
        {
            using var scope = scopeFactory.CreateScope();
            var applied = await scope.ServiceProvider.GetRequiredService<TaxonomyDefaults>().ApplyAsync();
            keyValues.SetValue(AppliedKey, DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture));
            logger.LogInformation("Set {Count} missing tag kinds and category icons", applied);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Setting the missing tag kinds and category icons failed; the next start tries again");
        }
    }
}
