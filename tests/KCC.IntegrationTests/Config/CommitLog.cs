using System.Collections.Concurrent;
using System.Data.Common;
using System.Diagnostics;
using System.Text;
using Microsoft.Data.Sqlite;
using NPoco;
using SQLitePCL;
using Umbraco.Cms.Persistence.Sqlite.Interceptors;

namespace KCC.IntegrationTests.Config;

// A transaction that has read fails with "database is locked" when another connection committed after its read, and
// that other connection is gone by the time the error surfaces, ten minutes later. Recording who committed on each
// of Umbraco's connections lets a failing test name the writer.
public sealed class CommitLog : SqliteConnectionInterceptor
{
    private const int Capacity = 64;

    private static readonly ConcurrentQueue<Commit> Recent = new();

    private static readonly delegate_commit RecordCommit = state =>
    {
        Recent.Enqueue(new Commit(DateTimeOffset.UtcNow, Environment.CurrentManagedThreadId, new StackTrace(1, false).ToString()));
        while (Recent.Count > Capacity && Recent.TryDequeue(out _))
        {
        }

        return 0;
    };

    // Registered ahead of Umbraco's own interceptors, which wrap the connection, so this one is handed the
    // SqliteConnection itself.
    public override DbConnection OnConnectionOpened(IDatabase database, DbConnection conn)
    {
        if (conn is SqliteConnection { Handle: { } handle })
        {
            raw.sqlite3_commit_hook(handle, RecordCommit, null);
        }

        return conn;
    }

    [AfterEvery(Test)]
    public static void ReportOnLockFailure(TestContext context)
    {
        if (LockFailure(context.Execution.Result?.Exception) is not { } locked)
        {
            return;
        }

        var report = new StringBuilder()
            .AppendLine($"SQLite error {locked.SqliteErrorCode}, extended code {locked.SqliteExtendedErrorCode}. Recent commits on Umbraco's connections, oldest first:");
        foreach (var commit in Recent.ToArray())
        {
            _ = report.AppendLine($"{commit.At:O} on thread {commit.Thread}").AppendLine(commit.Stack);
        }

        context.Output.WriteLine(report.ToString());
    }

    private static SqliteException? LockFailure(Exception? exception) => exception switch
    {
        null => null,
        SqliteException { SqliteErrorCode: raw.SQLITE_BUSY } locked => locked,
        AggregateException aggregate => aggregate.InnerExceptions.Select(LockFailure).FirstOrDefault(found => found is not null),
        _ => LockFailure(exception.InnerException),
    };

    private sealed record Commit(DateTimeOffset At, int Thread, string Stack);
}
