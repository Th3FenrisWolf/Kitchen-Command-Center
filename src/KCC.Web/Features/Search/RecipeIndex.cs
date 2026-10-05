using Lucene.Net.Facet.Taxonomy;
using Lucene.Net.Search;

namespace KCC.Web.Features.Search;

public sealed class RecipeIndex : IDisposable
{
    private readonly ReaderWriterLockSlim swap = new();
    private RecipeIndexSnapshot current = RecipeIndexBuilder.Build([]);
    private long version;

    public long Version => Interlocked.Read(ref version);

    public T Search<T>(Func<IndexSearcher, TaxonomyReader, T> search)
    {
        swap.EnterReadLock();
        try
        {
            return search(current.Searcher, current.Taxonomy);
        }
        finally
        {
            swap.ExitReadLock();
        }
    }

    // A rebuild builds its snapshot off to the side, so only the swap waits for searches in flight; no search sees
    // a half-built index, and no snapshot is disposed while a search still reads it.
    public void Replace(RecipeIndexSnapshot next)
    {
        RecipeIndexSnapshot previous;
        swap.EnterWriteLock();
        try
        {
            previous = current;
            current = next;
            Interlocked.Increment(ref version);
        }
        finally
        {
            swap.ExitWriteLock();
        }

        previous.Dispose();
    }

    public void Dispose()
    {
        current.Dispose();
        swap.Dispose();
    }
}
