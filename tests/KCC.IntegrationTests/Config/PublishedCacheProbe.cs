using System.Collections.Concurrent;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;

namespace KCC.IntegrationTests.Config;

// The recipe index rebuilds when this notification is handled, so what the published cache answers here is what a
// rebuild reads.
public sealed class PublishedCacheProbe(IUmbracoContextFactory umbracoContextFactory)
    : INotificationHandler<ContentCacheRefresherNotification>
{
    private readonly ConcurrentQueue<Sighting> sightings = new();

    public Sighting? LastSighting(Guid key) => sightings.LastOrDefault(sighting => sighting.Key == key);

    public void Handle(ContentCacheRefresherNotification notification)
    {
        if (notification.MessageObject is not ContentCacheRefresher.JsonPayload[] payloads)
        {
            return;
        }

        using var context = umbracoContextFactory.EnsureUmbracoContext();
        foreach (var key in payloads.Select(payload => payload.Key).OfType<Guid>())
        {
            var content = context.UmbracoContext.Content!.GetById(key);
            sightings.Enqueue(new Sighting(key, content?.Name, content?.Url()));
        }
    }

    public sealed record Sighting(Guid Key, string? PublishedName, string? PublishedUrl);
}
