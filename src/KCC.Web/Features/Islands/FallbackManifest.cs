using System.Collections;
using System.Text.Json;
using Vite.AspNetCore;

namespace KCC.Web.Features.Islands;

// Vite.AspNetCore reads only wwwroot/.vite/manifest.json; the fallback build writes its own beside its files.
// A manifest that is missing, unreadable or without the entry is not cached, so a build written later is picked up.
public sealed class FallbackManifest(IWebHostEnvironment environment, ILogger<FallbackManifest> logger) : IViteManifest
{
    internal const string Entry = "Features/Fallback.ts";

    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

    private Dictionary<string, Chunk> cached;
    private string lastWarning;

    public IEnumerable<string> Keys => Chunks().Keys;

    public IViteChunk this[string key] => Chunks().GetValueOrDefault(key);

    public bool ContainsKey(string key) => Chunks().ContainsKey(key);

    public IEnumerator<IViteChunk> GetEnumerator() => Chunks().Values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private Dictionary<string, Chunk> Chunks() => cached ?? Load();

    private Dictionary<string, Chunk> Load()
    {
        var path = Path.Combine(environment.WebRootPath ?? string.Empty, "fallback", ".vite", "manifest.json");
        var chunks = TryRead(path, out var problem);
        if (problem is null)
        {
            cached = chunks;
        }
        else
        {
            WarnOnce(problem);
        }

        return chunks;
    }

    private static Dictionary<string, Chunk> TryRead(string path, out string problem)
    {
        if (!File.Exists(path))
        {
            problem = $"The fallback manifest {path} is missing. Run `pnpm build:all`.";
            return [];
        }

        try
        {
            var chunks = JsonSerializer.Deserialize<Dictionary<string, Chunk>>(File.ReadAllText(path), Options) ?? [];
            problem = chunks.ContainsKey(Entry) ? null : $"The fallback manifest {path} has no {Entry} entry.";
            return chunks;
        }
        catch (Exception exception) when (exception is JsonException or IOException or UnauthorizedAccessException)
        {
            problem = $"The fallback manifest {path} is unreadable: {exception.Message}";
            return [];
        }
    }

    private void WarnOnce(string problem)
    {
        if (Interlocked.Exchange(ref lastWarning, problem) != problem)
        {
            logger.LogWarning("{Problem} Pages the render sidecar cannot serve will load without a script.", problem);
        }
    }

    private sealed record Chunk : IViteChunk
    {
        public string Src { get; init; }

        public string File { get; init; }

        public string Name { get; init; }

        public bool? IsEntry { get; init; }

        public bool? IsDynamicEntry { get; init; }

        public IEnumerable<string> Imports { get; init; }

        public IEnumerable<string> DynamicImports { get; init; }

        public IEnumerable<string> Css { get; init; }

        public IEnumerable<string> Assets { get; init; }
    }
}
