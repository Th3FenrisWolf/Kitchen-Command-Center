using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Scoping;

namespace KCC.Web.Features.Sqlite;

public interface IMemberWriteLock
{
    Task RunAsync(Func<Task> write);

    Task<T> RunAsync<T>(Func<Task<T>> write);
}

// Signing in, signing out and a failed attempt each save the member, and Umbraco's member store reads the member
// before it saves it, in one transaction. On SQLite that transaction cannot become the writer once another
// connection has committed, so it would retry for minutes behind a review write. Holding the member write lock from
// the start makes it the writer before it reads.
public class MemberWriteLock(ICoreScopeProvider scopeProvider) : IMemberWriteLock
{
    public Task RunAsync(Func<Task> write) => RunAsync(async () =>
    {
        await write();
        return true;
    });

    public async Task<T> RunAsync<T>(Func<Task<T>> write)
    {
        using var scope = scopeProvider.CreateCoreScope();
        scope.WriteLock(Constants.Locks.MemberTree);
        var result = await write();
        scope.Complete();
        return result;
    }
}
