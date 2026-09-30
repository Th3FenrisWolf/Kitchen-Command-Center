namespace KCC.IntegrationTests.Config;

// Commits a write from inside another transaction, as a second connection would, so a test can put a commit exactly
// where a transaction that has only read cannot survive one.
public static class OtherConnection
{
    // Ample for a commit nothing holds back, and well inside the five seconds a blocked one waits for the lock.
    private static readonly TimeSpan Wait = TimeSpan.FromSeconds(1);

    public static Task Commit(Func<Task> write)
    {
        // Inheriting the ambient scope would put the write inside the caller's own transaction.
        Task committed;
        using (ExecutionContext.SuppressFlow())
        {
            committed = Task.Run(write);
        }

        // Behind the caller's write lock the commit cannot finish until the caller has.
        SpinWait.SpinUntil(() => committed.IsCompleted, Wait);
        return committed;
    }
}
