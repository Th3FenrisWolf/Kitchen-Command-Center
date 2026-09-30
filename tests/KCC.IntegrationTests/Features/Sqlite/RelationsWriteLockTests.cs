using KCC.IntegrationTests.Config;
using KCC.Web.Features.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;

namespace KCC.IntegrationTests.Features.Sqlite;

public class RelationsWriteLockTests
{
    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    [Test]
    public async Task EveryRelationsHandler_TakesTheWriteLock()
    {
        _ = await Assert.That(IsWrapped<ContentSavedNotification>()).IsTrue();
        _ = await Assert.That(IsWrapped<ContentPublishedNotification>()).IsTrue();
        _ = await Assert.That(IsWrapped<ContentUnpublishedNotification>()).IsTrue();
        _ = await Assert.That(IsWrapped<MediaSavedNotification>()).IsTrue();
        _ = await Assert.That(IsWrapped<MemberSavedNotification>()).IsTrue();
    }

    [Test]
    public async Task EveryRelateOnTrashHandler_TakesTheWriteLock()
    {
        _ = await Assert.That(IsTrashWrapped<ContentMovedToRecycleBinNotification>()).IsTrue();
        _ = await Assert.That(IsTrashWrapped<MediaMovedToRecycleBinNotification>()).IsTrue();
    }

    [Test]
    public async Task APickedCategory_IsStillRecordedAsARelation()
    {
        var category = await TestContent.CategoryAsync(Site.Services, "IT Pangolin Shelf");
        try
        {
            var recipe = await TestContent.RecipeAsync(Site.Services, "IT Pangolin", TestContent.Pick("category", category));

            using var scope = Site.Services.CreateScope();
            var content = scope.ServiceProvider.GetRequiredService<IContentService>();
            var relations = scope.ServiceProvider.GetRequiredService<IRelationService>().GetByParentId(content.GetById(recipe)!.Id) ?? [];

            _ = await Assert.That(relations.Select(relation => relation.ChildId)).Contains(content.GetById(category)!.Id);
        }
        finally
        {
            await TestContent.TrashAsync(Site.Services, category);
        }
    }

    private bool IsWrapped<TNotification>()
        where TNotification : INotification
    {
        using var scope = Site.Services.CreateScope();
        var handlers = scope.ServiceProvider.GetServices<INotificationHandler<TNotification>>().ToList();
        return handlers.OfType<WriteLockedRelationsUpdate<TNotification>>().Count() == 1
            && handlers.All(handler => handler.GetType().FullName != SqliteComposer.RelationsHandlerTypeName);
    }

    private bool IsTrashWrapped<TNotification>()
        where TNotification : INotification
    {
        using var scope = Site.Services.CreateScope();
        var handlers = scope.ServiceProvider.GetServices<INotificationAsyncHandler<TNotification>>().ToList();
        return handlers.OfType<WriteLockedRelateOnTrash<TNotification>>().Count() == 1
            && !handlers.OfType<RelateOnTrashNotificationHandler>().Any();
    }
}
