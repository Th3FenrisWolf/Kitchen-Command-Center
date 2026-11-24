using Vite.AspNetCore;

namespace KCC.Web.Features.Islands;

public sealed record FallbackLinks(string Script, IReadOnlyList<string> Stylesheets, bool FromDevServer)
{
    private const string Entry = FallbackManifest.Entry;
    private const string Folder = "fallback/";

    public static FallbackLinks For(IViteManifest manifest, bool devServer)
    {
        if (devServer)
        {
            return new FallbackLinks(Entry, [], FromDevServer: true);
        }

        if (!manifest.ContainsKey(Entry))
        {
            return new FallbackLinks(null, [], FromDevServer: false);
        }

        var stylesheets = new List<string>();
        CollectCss(manifest, Entry, new HashSet<string>(), stylesheets);

        return new FallbackLinks(
            Folder + manifest[Entry].File,
            stylesheets.ConvertAll(css => Folder + css),
            FromDevServer: false);
    }

    // Imports first, as Vite orders them, so a component's rules still win over a shared chunk's at equal specificity.
    private static void CollectCss(IViteManifest manifest, string key, HashSet<string> visited, List<string> into)
    {
        if (!visited.Add(key) || !manifest.ContainsKey(key))
        {
            return;
        }

        var chunk = manifest[key];
        foreach (var import in chunk.Imports ?? [])
        {
            CollectCss(manifest, import, visited, into);
        }

        foreach (var css in chunk.Css ?? [])
        {
            if (!into.Contains(css))
            {
                into.Add(css);
            }
        }
    }
}
