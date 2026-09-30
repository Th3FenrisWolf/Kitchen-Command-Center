using KCC.IntegrationTests.Config;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Services;

namespace KCC.IntegrationTests.Features.Hosting;

public class BootTests
{
    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    [Test]
    public async Task FreshDatabase_ReachesTheRunLevel()
    {
        var state = Site.Services.GetRequiredService<IRuntimeState>();

        _ = await Assert.That(state.Level).IsEqualTo(RuntimeLevel.Run);
    }

    [Test]
    public async Task FreshDatabase_UsesWriteAheadLogging()
    {
        await using var connection = new SqliteConnection($"Data Source={Site.DatabasePath};Pooling=False");
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA journal_mode;";

        _ = await Assert.That((string?)await command.ExecuteScalarAsync()).IsEqualTo("wal");
    }
}
