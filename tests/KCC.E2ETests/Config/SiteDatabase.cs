using Microsoft.Data.Sqlite;

namespace KCC.E2ETests.Config;

/// <summary>Reads and writes a stopped site's SQLite file directly, the way a live edit would change it.</summary>
public static class SiteDatabase
{
    private const string DocumentObjectType = "C66BA18E-EAF3-4CFF-8A22-41B16D66A972";

    public static async Task<string?> ReadDictionaryValueAsync(string databasePath, string key)
    {
        await using var connection = await OpenAsync(databasePath);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT t.value FROM cmsLanguageText t
            INNER JOIN cmsDictionary d ON t.UniqueId = d.id
            WHERE d."key" = $key
            """;
        command.Parameters.AddWithValue("$key", key);
        return (string?)await command.ExecuteScalarAsync();
    }

    public static async Task WriteDictionaryValueAsync(string databasePath, string key, string value)
    {
        await using var connection = await OpenAsync(databasePath);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE cmsLanguageText SET value = $value
            WHERE UniqueId = (SELECT id FROM cmsDictionary WHERE "key" = $key)
            """;
        command.Parameters.AddWithValue("$key", key);
        command.Parameters.AddWithValue("$value", value);
        if (await command.ExecuteNonQueryAsync() != 1)
        {
            throw new InvalidOperationException($"Dictionary item {key} has no translation row to update.");
        }
    }

    public static async Task<long> FindDocumentIdAsync(string databasePath, string name)
    {
        await using var connection = await OpenAsync(databasePath);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id FROM umbracoNode
            WHERE text = $name AND nodeObjectType = $documentType COLLATE NOCASE
            """;
        command.Parameters.AddWithValue("$name", name);
        command.Parameters.AddWithValue("$documentType", DocumentObjectType);
        return (long?)await command.ExecuteScalarAsync() ?? throw new InvalidOperationException($"No document is named {name}.");
    }

    public static async Task<string?> ReadDocumentNameAsync(string databasePath, long id)
    {
        await using var connection = await OpenAsync(databasePath);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT text FROM umbracoNode WHERE id = $id";
        command.Parameters.AddWithValue("$id", id);
        return (string?)await command.ExecuteScalarAsync();
    }

    public static async Task RenameDocumentAsync(string databasePath, long id, string name)
    {
        await using var connection = await OpenAsync(databasePath);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE umbracoNode SET text = $name WHERE id = $id;
            UPDATE umbracoContentVersion SET text = $name WHERE nodeId = $id AND "current" = 1;
            """;
        command.Parameters.AddWithValue("$id", id);
        command.Parameters.AddWithValue("$name", name);
        if (await command.ExecuteNonQueryAsync() != 2)
        {
            throw new InvalidOperationException($"Document {id} has no node and current version to rename.");
        }
    }

    private static async Task<SqliteConnection> OpenAsync(string databasePath)
    {
        var connection = new SqliteConnection($"Data Source={databasePath};Pooling=False");
        await connection.OpenAsync();
        return connection;
    }
}
