using Microsoft.Data.Sqlite;

namespace KCC.E2ETests.Config;

public static class SiteDatabase
{
    private const string DocumentObjectType = "C66BA18E-EAF3-4CFF-8A22-41B16D66A972";

    public static async Task<string?> ReadDictionaryValueAsync(string databasePath, string key) =>
        (string?)await ScalarAsync(databasePath, """
            SELECT t.value FROM cmsLanguageText t
            INNER JOIN cmsDictionary d ON t.UniqueId = d.id
            WHERE d."key" = $key
            """, ("$key", key));

    public static async Task WriteDictionaryValueAsync(string databasePath, string key, string value)
    {
        var updated = await ExecuteAsync(databasePath, """
            UPDATE cmsLanguageText SET value = $value
            WHERE UniqueId = (SELECT id FROM cmsDictionary WHERE "key" = $key)
            """, ("$key", key), ("$value", value));
        if (updated != 1)
        {
            throw new InvalidOperationException($"Dictionary item {key} has no translation row to update.");
        }
    }

    public static async Task DeleteDictionaryItemAsync(string databasePath, string key)
    {
        var deleted = await ExecuteAsync(databasePath, """
            DELETE FROM cmsLanguageText WHERE UniqueId = (SELECT id FROM cmsDictionary WHERE "key" = $key);
            DELETE FROM cmsDictionary WHERE "key" = $key;
            """, ("$key", key));
        if (deleted != 2)
        {
            throw new InvalidOperationException($"Dictionary item {key} has no single translation and item row to delete.");
        }
    }

    public static async Task<long> FindDocumentIdAsync(string databasePath, string name) =>
        (long?)await ScalarAsync(databasePath, """
            SELECT id FROM umbracoNode
            WHERE text = $name AND nodeObjectType = $documentType COLLATE NOCASE
            """, ("$name", name), ("$documentType", DocumentObjectType))
        ?? throw new InvalidOperationException($"No document is named {name}.");

    public static async Task<string?> ReadDocumentNameAsync(string databasePath, long id) =>
        (string?)await ScalarAsync(databasePath, "SELECT text FROM umbracoNode WHERE id = $id", ("$id", id));

    public static async Task RenameDocumentAsync(string databasePath, long id, string name)
    {
        var renamed = await ExecuteAsync(databasePath, """
            UPDATE umbracoNode SET text = $name WHERE id = $id;
            UPDATE umbracoContentVersion SET text = $name WHERE nodeId = $id AND "current" = 1;
            """, ("$id", id), ("$name", name));
        if (renamed != 2)
        {
            throw new InvalidOperationException($"Document {id} has no node and current version to rename.");
        }
    }

    private static Task<object?> ScalarAsync(string databasePath, string sql, params (string Name, object Value)[] parameters) =>
        RunAsync(databasePath, sql, parameters, command => command.ExecuteScalarAsync());

    private static Task<int> ExecuteAsync(string databasePath, string sql, params (string Name, object Value)[] parameters) =>
        RunAsync(databasePath, sql, parameters, command => command.ExecuteNonQueryAsync());

    private static async Task<T> RunAsync<T>(
        string databasePath, string sql, (string Name, object Value)[] parameters, Func<SqliteCommand, Task<T>> run)
    {
        await using var connection = new SqliteConnection($"Data Source={databasePath};Pooling=False");
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value);
        }

        return await run(command);
    }
}
