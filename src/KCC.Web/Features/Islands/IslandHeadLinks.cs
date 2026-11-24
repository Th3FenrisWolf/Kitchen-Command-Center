using KCC.Web.Features.Models.Common;
using Vite.AspNetCore;

namespace KCC.Web.Features.Islands;

public static class IslandHeadLinks
{
    private const string Entry = "Features/Main.ts";
    private const string EntryStylesheet = "Features/Styles/Main.css";
    private const string ComponentSuffix = ".Component.vue";

    public static HeadLinks For(SsrResult result, IViteManifest manifest)
    {
        if (result.Html is null)
        {
            return HeadLinks.None;
        }

        var linkedByEntry = new HashSet<string>();
        if (Chunk(manifest, EntryStylesheet) is { File: var entryStylesheet })
        {
            linkedByEntry.Add(entryStylesheet);
        }

        CollectCss(manifest, Entry, new HashSet<string>(), linkedByEntry, []);

        var stylesheets = new List<string>();
        var cssVisited = new HashSet<string>();
        foreach (var component in result.Components)
        {
            CollectCss(manifest, component, cssVisited, linkedByEntry, stylesheets);
        }

        var entryFile = Chunk(manifest, Entry)?.File;
        var preloads = new List<string>();
        if (entryFile is not null)
        {
            preloads.Add(entryFile);
        }

        var preloadVisited = new HashSet<string>();
        foreach (var island in result.Islands)
        {
            if (result.Components.FirstOrDefault(path => IsComponent(path, island.Name)) is { } path)
            {
                CollectChunks(manifest, path, preloadVisited, preloads, entryFile);
            }
        }

        return new HeadLinks(stylesheets, preloads);
    }

    // Vite.AspNetCore's indexer logs a warning on a missing key, and most component paths have none: a server-only
    // component or one folded into a shared chunk.
    private static IViteChunk Chunk(IViteManifest manifest, string key) =>
        manifest.ContainsKey(key) ? manifest[key] : null;

    private static bool IsComponent(string path, string name) =>
        path.EndsWith($"/{name}{ComponentSuffix}", StringComparison.Ordinal)
        || path == $"{name}{ComponentSuffix}";

    // A chunk's imports come before its own CSS, as Vite orders them, so a component's rules still win over a
    // shared chunk's at equal specificity.
    private static void CollectCss(
        IViteManifest manifest,
        string key,
        HashSet<string> visited,
        HashSet<string> linked,
        List<string> into)
    {
        if (!visited.Add(key) || Chunk(manifest, key) is not { } chunk)
        {
            return;
        }

        foreach (var import in chunk.Imports ?? [])
        {
            CollectCss(manifest, import, visited, linked, into);
        }

        foreach (var css in chunk.Css ?? [])
        {
            if (linked.Add(css))
            {
                into.Add(css);
            }
        }
    }

    private static void CollectChunks(
        IViteManifest manifest,
        string key,
        HashSet<string> visited,
        List<string> into,
        string entryFile)
    {
        if (!visited.Add(key) || Chunk(manifest, key) is not { } chunk)
        {
            return;
        }

        if (chunk.File != entryFile)
        {
            into.Add(chunk.File);
        }

        foreach (var import in chunk.Imports ?? [])
        {
            CollectChunks(manifest, import, visited, into, entryFile);
        }
    }
}
