using System.Diagnostics;
using System.Threading.Channels;
using KCC.Web.Features.Search;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Services;

namespace KCC.UnitTests.Features.Search;

public class RecipeIndexRebuilderTests
{
    private static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(50);

    [Test]
    public async Task Start_BuildsTheIndexUnasked()
    {
        var source = new FakeSource("Chili");
        await using var site = await StartAsync(source);

        await site.Rebuilder.WhenCurrentAsync(CancellationToken.None);

        _ = await Assert.That(source.Loads).IsEqualTo(1);
        _ = await Assert.That(site.RecipeCount()).IsEqualTo(1);
    }

    [Test]
    public async Task ABurstOfSignals_CostsOneRebuild()
    {
        var source = new FakeSource("Chili");
        await using var site = await StartAsync(source);
        await site.Rebuilder.WhenCurrentAsync(CancellationToken.None);

        for (var signal = 0; signal < 5; signal++)
        {
            site.Rebuilder.Signal();
        }

        await site.Rebuilder.WhenCurrentAsync(CancellationToken.None);

        _ = await Assert.That(source.Loads).IsEqualTo(2);
    }

    [Test]
    public async Task WhenCurrent_WaitsForTheSignalledChange()
    {
        var source = new FakeSource("Chili");
        await using var site = await StartAsync(source);
        await site.Rebuilder.WhenCurrentAsync(CancellationToken.None);

        source.Names = ["Chili", "Stew"];
        site.Rebuilder.Signal();
        await site.Rebuilder.WhenCurrentAsync(CancellationToken.None);

        _ = await Assert.That(site.RecipeCount()).IsEqualTo(2);
    }

    [Test]
    public async Task AFailedRebuild_KeepsTheLastIndexAndTheNextSignalRetries()
    {
        var source = new FakeSource("Chili");
        await using var site = await StartAsync(source);
        await site.Rebuilder.WhenCurrentAsync(CancellationToken.None);

        source.Names = ["Chili", "Stew"];
        source.Failure = new InvalidOperationException("The published cache is unavailable.");
        site.Rebuilder.Signal();

        _ = await Assert.That(async () => await site.Rebuilder.WhenCurrentAsync(CancellationToken.None))
            .Throws<InvalidOperationException>();
        _ = await Assert.That(site.RecipeCount()).IsEqualTo(1);

        source.Failure = null;
        site.Rebuilder.Signal();
        await site.Rebuilder.WhenCurrentAsync(CancellationToken.None);

        _ = await Assert.That(site.RecipeCount()).IsEqualTo(2);
    }

    [Test]
    public async Task AFailedRebuild_FailsAWaiterThatArrivesAfterIt()
    {
        var source = new FakeSource("Chili");
        await using var site = await StartAsync(source);
        await site.Rebuilder.WhenCurrentAsync(CancellationToken.None);

        source.Failure = new InvalidOperationException("The published cache is unavailable.");
        site.Rebuilder.Signal();
        _ = await Assert.That(async () => await site.Rebuilder.WhenCurrentAsync(CancellationToken.None))
            .Throws<InvalidOperationException>();

        _ = await Assert.That(async () => await site.Rebuilder.WhenCurrentAsync(new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token))
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task AFailedStartupBuild_IsRetriedWithoutASignal()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var source = new FakeSource("Chili") { Failure = new InvalidOperationException("The published cache is unavailable.") };
        await using var site = await StartAsync(source);
        _ = await Assert.That(async () => await site.Rebuilder.WhenCurrentAsync(timeout.Token))
            .Throws<InvalidOperationException>();

        source.Failure = null;
        await source.Succeeded.WaitAsync(timeout.Token);
        await site.Rebuilder.WhenCurrentAsync(timeout.Token);

        _ = await Assert.That(site.RecipeCount()).IsEqualTo(1);
    }

    [Test]
    public async Task ConsecutiveFailures_BackOff()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var source = new FakeSource("Chili") { Failure = new InvalidOperationException("The published cache is unavailable.") };
        await using var site = await StartAsync(source);

        var loadTimes = new List<TimeSpan>();
        while (loadTimes.Count < 5)
        {
            loadTimes.Add(await source.LoadTimes.ReadAsync(timeout.Token));
        }

        // The retries should come RetryDelay × 1, 2, 4 and 8 apart. Only lower bounds, at half those, are asserted: a
        // busy machine stretches a gap, but no timer fires that early.
        for (var gap = 0; gap < 4; gap++)
        {
            _ = await Assert.That(loadTimes[gap + 1] - loadTimes[gap]).IsGreaterThanOrEqualTo(RetryDelay * (1 << gap) / 2);
        }
    }

    [Test]
    public async Task BeforeInstall_BuildsNothing()
    {
        var source = new FakeSource("Chili");
        await using var site = await StartAsync(source, RuntimeLevel.Install);

        await site.Rebuilder.WhenCurrentAsync(CancellationToken.None);

        _ = await Assert.That(source.Loads).IsEqualTo(0);
    }

    private static async Task<RunningRebuilder> StartAsync(FakeSource source, RuntimeLevel level = RuntimeLevel.Run)
    {
        var services = new ServiceCollection().AddScoped<IRecipeIndexSource>(_ => source).BuildServiceProvider();
        var runtimeState = new Mock<IRuntimeState>();
        runtimeState.Setup(state => state.Level).Returns(level);
        var index = new RecipeIndex();
        var rebuilder = new RecipeIndexRebuilder(
            services.GetRequiredService<IServiceScopeFactory>(),
            index,
            runtimeState.Object,
            Options.Create(new RecipeSearchOptions { RebuildDelay = TimeSpan.FromMilliseconds(50), RetryDelay = RetryDelay }),
            NullLogger<RecipeIndexRebuilder>.Instance);
        await rebuilder.StartAsync(CancellationToken.None);
        return new RunningRebuilder(rebuilder, index, services);
    }

    private sealed class RunningRebuilder(RecipeIndexRebuilder rebuilder, RecipeIndex index, ServiceProvider services) : IAsyncDisposable
    {
        public RecipeIndexRebuilder Rebuilder => rebuilder;

        public int RecipeCount() => index.Search((searcher, _) => searcher.IndexReader.NumDocs);

        public async ValueTask DisposeAsync()
        {
            await rebuilder.StopAsync(CancellationToken.None);
            rebuilder.Dispose();
            index.Dispose();
            await services.DisposeAsync();
        }
    }

    private sealed class FakeSource(params string[] names) : IRecipeIndexSource
    {
        private readonly long created = Stopwatch.GetTimestamp();
        private readonly Channel<TimeSpan> loadTimes = Channel.CreateUnbounded<TimeSpan>();
        private readonly TaskCompletionSource succeeded = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int loads;

        public string[] Names { get; set; } = names;

        public Exception Failure { get; set; }

        public int Loads => Volatile.Read(ref loads);

        public ChannelReader<TimeSpan> LoadTimes => loadTimes.Reader;

        public Task Succeeded => succeeded.Task;

        public Task<IReadOnlyList<RecipeSearchDocument>> LoadAsync()
        {
            Interlocked.Increment(ref loads);
            _ = loadTimes.Writer.TryWrite(Stopwatch.GetElapsedTime(created));
            var failure = Failure;
            if (failure is not null)
            {
                return Task.FromException<IReadOnlyList<RecipeSearchDocument>>(failure);
            }

            _ = succeeded.TrySetResult();
            return Task.FromResult<IReadOnlyList<RecipeSearchDocument>>(Names.Select(name => new RecipeSearchDocument { Name = name }).ToList());
        }
    }
}
