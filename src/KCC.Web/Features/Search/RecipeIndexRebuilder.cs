using System.Diagnostics;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Services;

namespace KCC.Web.Features.Search;

public interface IRecipeIndexRebuilder
{
    void Signal();

    Task WhenCurrentAsync(CancellationToken cancellationToken);
}

public sealed class RecipeIndexRebuilder(
    IServiceScopeFactory scopeFactory,
    RecipeIndex index,
    IRuntimeState runtimeState,
    IOptions<RecipeSearchOptions> options,
    ILogger<RecipeIndexRebuilder> logger) : BackgroundService, IRecipeIndexRebuilder
{
    private static readonly TimeSpan MaxRetryDelay = TimeSpan.FromMinutes(10);

    private readonly SemaphoreSlim wake = new(0);
    private readonly Lock gate = new();
    private readonly List<(long Generation, TaskCompletionSource Done)> waiters = [];
    private long requested;
    private long built;
    private long? failedGeneration;
    private Exception failedException;
    private TimeSpan retryDelay = Timeout.InfiniteTimeSpan;

    public void Signal()
    {
        Interlocked.Increment(ref requested);
        wake.Release();
    }

    public Task WhenCurrentAsync(CancellationToken cancellationToken)
    {
        lock (gate)
        {
            var generation = Interlocked.Read(ref requested);
            if (built >= generation)
            {
                return Task.CompletedTask;
            }

            if (generation <= failedGeneration)
            {
                return Task.FromException(failedException);
            }

            var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            waiters.Add((generation, done));
            return done.Task.WaitAsync(cancellationToken);
        }
    }

    // Signalled here rather than in ExecuteAsync, which .NET 10 starts on a background thread: anyone waiting from
    // start-up on must wait for the first build.
    public override Task StartAsync(CancellationToken cancellationToken)
    {
        Signal();
        return base.StartAsync(cancellationToken);
    }

    public override void Dispose()
    {
        wake.Dispose();
        base.Dispose();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // The first build skips the quiet period, so a restarted site is searchable at once.
        var quietPeriod = TimeSpan.Zero;
        while (!stoppingToken.IsCancellationRequested)
        {
            if (await wake.WaitAsync(retryDelay, stoppingToken))
            {
                // Saves arrive in bursts (a publish with descendants, the seeder); each new signal restarts the
                // quiet period, so a burst costs one rebuild, but a stream that never goes quiet — sign-ins saving
                // the member, say — still rebuilds within MaxRebuildWait of the burst's first signal.
                var burstStarted = Stopwatch.GetTimestamp();
                TimeSpan remaining;
                while ((remaining = options.Value.MaxRebuildWait - Stopwatch.GetElapsedTime(burstStarted)) > TimeSpan.Zero
                    && await wake.WaitAsync(quietPeriod < remaining ? quietPeriod : remaining, stoppingToken))
                {
                }
            }
            else
            {
                // Counted as a signal, so a waiter from here on waits for the retry instead of failing on the build
                // before it.
                Interlocked.Increment(ref requested);
            }

            quietPeriod = options.Value.RebuildDelay;
            await RebuildAsync(Interlocked.Read(ref requested), stoppingToken);
        }
    }

    private async Task RebuildAsync(long generation, CancellationToken stoppingToken)
    {
        try
        {
            // An install or upgrade boot has no content to read yet.
            if (runtimeState.Level == RuntimeLevel.Run)
            {
                var started = Stopwatch.GetTimestamp();
                using var scope = scopeFactory.CreateScope();
                var documents = await scope.ServiceProvider.GetRequiredService<IRecipeIndexSource>().LoadAsync();
                index.Replace(RecipeIndexBuilder.Build(documents));
                logger.LogInformation(
                    "Rebuilt the recipe index with {RecipeCount} recipes in {ElapsedMs:0} ms",
                    documents.Count,
                    Stopwatch.GetElapsedTime(started).TotalMilliseconds);
            }

            retryDelay = Timeout.InfiniteTimeSpan;
            Complete(generation, null);
        }
        catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
        {
            // An unhandled exception would stop the host. The previous index keeps serving until a retry or the next
            // change rebuilds it.
            retryDelay = retryDelay == Timeout.InfiniteTimeSpan ? options.Value.RetryDelay : retryDelay * 2;
            if (retryDelay > MaxRetryDelay)
            {
                retryDelay = MaxRetryDelay;
            }

            logger.LogError(exception, "Rebuilding the recipe index failed; retrying in {RetryDelay}", retryDelay);
            Complete(generation, exception);
        }
    }

    private void Complete(long generation, Exception failure)
    {
        lock (gate)
        {
            if (failure is null)
            {
                built = Math.Max(built, generation);
            }
            else
            {
                // A later call for this generation fails at once instead of waiting out the retry's backoff.
                failedGeneration = generation;
                failedException = failure;
            }

            foreach (var waiter in waiters.Where(waiter => waiter.Generation <= generation).ToList())
            {
                if (failure is null)
                {
                    waiter.Done.TrySetResult();
                }
                else
                {
                    waiter.Done.TrySetException(failure);
                }

                waiters.Remove(waiter);
            }
        }
    }
}
