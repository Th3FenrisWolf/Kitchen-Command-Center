using System.Collections;
using KCC.Web.Features.Islands;
using TUnit.Assertions.Enums;
using Vite.AspNetCore;

namespace KCC.UnitTests.Features.Islands;

public class FallbackLinksTests
{
    private static readonly FixtureManifest Manifest = new(
        new FixtureChunk("Features/Fallback.ts", "assets/fallback-f1.js", IsEntry: true, Css: ["assets/fallback-f1.css"], Imports: ["_vue-v1.js", "_KccSheet-k1.js"]),
        new FixtureChunk("_vue-v1.js", "assets/vue-v1.js"),
        new FixtureChunk("_KccSheet-k1.js", "assets/KccSheet-k1.js", Css: ["assets/KccSheet-k1.css", "assets/fallback-f1.css"], Imports: ["_vue-v1.js", "_tear-t1.js"]),
        new FixtureChunk("_tear-t1.js", "assets/tear-t1.js", Css: ["assets/tear-t1.css"]),
        new FixtureChunk("Features/Components/Header/AppHeader.Component.vue", "assets/AppHeader-h1.js", IsDynamicEntry: true, Css: ["assets/AppHeader-h1.css"]));

    [Test]
    public async Task For_LinksTheEntryScriptFromTheFallbackFolder()
    {
        var links = FallbackLinks.For(Manifest, devServer: false);

        _ = await Assert.That(links.Script).IsEqualTo("fallback/assets/fallback-f1.js");
        _ = await Assert.That(links.FromDevServer).IsFalse();
    }

    [Test]
    public async Task For_LinksEveryCssFileOfTheEntrysClosure_OnceAndImportsFirst()
    {
        var links = FallbackLinks.For(Manifest, devServer: false);

        _ = await Assert.That(links.Stylesheets).IsEquivalentTo(
            ["fallback/assets/tear-t1.css", "fallback/assets/KccSheet-k1.css", "fallback/assets/fallback-f1.css"],
            CollectionOrdering.Matching);
    }

    [Test]
    public async Task For_LinksNoCssOfAChunkTheEntryNeverImports()
    {
        var links = FallbackLinks.For(Manifest, devServer: false);

        _ = await Assert.That(links.Stylesheets).DoesNotContain("fallback/assets/AppHeader-h1.css");
    }

    [Test]
    public async Task For_TheDevServer_LinksTheEntryModuleItServes_AndNothingFromTheFallbackFolder()
    {
        var links = FallbackLinks.For(new FixtureManifest(), devServer: true);

        _ = await Assert.That(links.Script).IsEqualTo("Features/Fallback.ts");
        _ = await Assert.That(links.FromDevServer).IsTrue();
        _ = await Assert.That(links.Stylesheets).IsEmpty();
    }

    [Test]
    public async Task For_AManifestWithoutTheEntry_LinksNothing()
    {
        var manifest = new FixtureManifest();

        var links = FallbackLinks.For(manifest, devServer: false);

        _ = await Assert.That(links.Script).IsNull();
        _ = await Assert.That(links.Stylesheets).IsEmpty();
        _ = await Assert.That(manifest.Misses).IsEmpty();
    }

    private sealed record FixtureChunk(
        string Src,
        string File,
        bool IsEntry = false,
        bool IsDynamicEntry = false,
        IEnumerable<string> Css = null,
        IEnumerable<string> Imports = null) : IViteChunk
    {
        public IEnumerable<string> Assets => null;

        public IEnumerable<string> DynamicImports => null;

        public string Name => null;

        bool? IViteChunk.IsEntry => IsEntry;

        bool? IViteChunk.IsDynamicEntry => IsDynamicEntry;
    }

    private sealed class FixtureManifest(params FixtureChunk[] chunks) : IViteManifest
    {
        private readonly Dictionary<string, IViteChunk> byKey = chunks.ToDictionary(c => c.Src, c => (IViteChunk)c);

        public List<string> Misses { get; } = [];

        public IViteChunk this[string key]
        {
            get
            {
                if (!byKey.TryGetValue(key, out var chunk))
                {
                    Misses.Add(key);
                }

                return chunk;
            }
        }

        public IEnumerable<string> Keys => byKey.Keys;

        public bool ContainsKey(string key) => byKey.ContainsKey(key);

        public IEnumerator<IViteChunk> GetEnumerator() => byKey.Values.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
