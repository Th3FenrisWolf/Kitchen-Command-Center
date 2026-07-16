using CMS.Core;
using KCC.Web.Features.Search;
using Kentico.Xperience.Lucene;
using Kentico.Xperience.Lucene.Core.Indexing;
using Moq;

namespace KCC.UnitTests.Features.Search;

public class RecipeReindexerTests
{
    // IndexEventWebPageItemModel has a public 12-arg positional ctor (the same shape
    // RecipeSearchIndexingStrategy.FindItemsToReindex constructs). It is a plain Lucene model,
    // not a Kentico Info, so it is directly constructable in a unit test.
    private static IndexEventWebPageItemModel Model(string language) => new(
        42, Guid.NewGuid(), language, "KCC.Recipe", "egg-skillet",
        false, 7, 1, "KCC", "/Recipes/Egg-Skillet", 0, 3);

    private static RecipeReindexer Build(
        Mock<IRecipeReindexTargetResolver> resolver,
        Mock<ILuceneTaskLogger> logger,
        Mock<IEventLogService> eventLog) =>
        new(resolver.Object, logger.Object, eventLog.Object);

    [Test]
    public async Task Enqueues_OneUpdateTaskPerResolvedTarget()
    {
        var en = Model("en");
        var es = Model("es");
        var resolver = new Mock<IRecipeReindexTargetResolver>();
        resolver
            .Setup(r => r.ResolveAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { en, es });
        var logger = new Mock<ILuceneTaskLogger>();
        var eventLog = new Mock<IEventLogService>();

        await Build(resolver, logger, eventLog).ReindexRecipeAsync(Guid.NewGuid());

        // Assert each resolved model flows through exactly once — a bug that enqueued the same
        // target twice (or dropped one) would fail the per-model Times.Once check.
        foreach (var expected in new[] { en, es })
        {
            logger.Verify(
                l => l.LogIndexTask(It.Is<LuceneQueueItem>(q =>
                    ReferenceEquals(q.ItemToIndex, expected)
                    && q.TaskType == LuceneTaskType.UPDATE
                    && q.IndexName == RecipeSearchConstants.IndexName)),
                Times.Once);
        }
    }

    [Test]
    public async Task NoOp_WhenNothingResolves()
    {
        var resolver = new Mock<IRecipeReindexTargetResolver>();
        resolver
            .Setup(r => r.ResolveAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<IndexEventWebPageItemModel>());
        var logger = new Mock<ILuceneTaskLogger>();
        var eventLog = new Mock<IEventLogService>();

        await Build(resolver, logger, eventLog).ReindexRecipeAsync(Guid.NewGuid());

        logger.Verify(l => l.LogIndexTask(It.IsAny<LuceneQueueItem>()), Times.Never);
    }

    [Test]
    public async Task SwallowsResolverFailure_AndLogsToEventLog()
    {
        var resolver = new Mock<IRecipeReindexTargetResolver>();
        resolver
            .Setup(r => r.ResolveAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("index boom"));
        var logger = new Mock<ILuceneTaskLogger>();
        var eventLog = new Mock<IEventLogService>();

        // Must NOT throw: the review write is already committed; a search hiccup can't surface.
        await Build(resolver, logger, eventLog).ReindexRecipeAsync(Guid.NewGuid());

        logger.Verify(l => l.LogIndexTask(It.IsAny<LuceneQueueItem>()), Times.Never);
        // LogException is an extension over LogEvent(EventLogData); verify the underlying call.
        eventLog.Verify(l => l.LogEvent(It.IsAny<EventLogData>()), Times.Once);
    }
}
