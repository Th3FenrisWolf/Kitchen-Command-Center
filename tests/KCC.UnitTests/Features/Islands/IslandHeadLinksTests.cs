using System.Collections;
using System.Text.Json;
using KCC.Web.Features.Islands;
using KCC.Web.Features.Models.Common;
using TUnit.Assertions.Enums;
using Vite.AspNetCore;

namespace KCC.UnitTests.Features.Islands;

public class IslandHeadLinksTests
{
    private const string Header = "Features/Components/Header/AppHeader.Component.vue";
    private const string Footer = "Features/Components/Footer/AppFooter.Component.vue";
    private const string Search = "Features/Pages/RecipeSearch/RecipeSearchView.Component.vue";

    private static readonly FixtureChunk[] Chunks =
    [
        new FixtureChunk("Features/Styles/Main.css", "assets/Main-a1.css"),
        new FixtureChunk("Features/Main.ts", "assets/main-e1.js", IsEntry: true, Css: ["assets/main-e1.css"], Imports: ["_vue-v1.js", "_entry-shared-s1.js"]),
        new FixtureChunk("_vue-v1.js", "assets/vue-v1.js"),
        new FixtureChunk("_entry-shared-s1.js", "assets/entry-shared-s1.js", Css: ["assets/entry-shared-s1.css"]),
        new FixtureChunk("_KccSheet-k1.js", "assets/KccSheet-k1.js", Css: ["assets/KccSheet-k1.css"], Imports: ["_vue-v1.js"]),
        new FixtureChunk(Header, "assets/AppHeader-h1.js", IsDynamicEntry: true, Css: ["assets/AppHeader-h1.css"], Imports: ["_vue-v1.js", "_KccSheet-k1.js", "_entry-shared-s1.js"]),
        new FixtureChunk(Search, "assets/RecipeSearchView-r1.js", IsDynamicEntry: true, Css: ["assets/RecipeSearchView-r1.css"], Imports: ["_KccSheet-k1.js", "_vue-v1.js"]),
    ];

    private static readonly FixtureManifest Manifest = new(Chunks);

    private static SsrResult Rendered() => new()
    {
        Html = "<kcc-island data-island=\"0\"></kcc-island>",
        Islands =
        [
            new IslandEntry("AppHeader", Props(), []),
            new IslandEntry("RecipeSearchView", Props(), []),
        ],
        Components = [Header, Footer, Search],
    };

    [Test]
    public async Task For_LinksTheCssOfEveryRenderedComponentAndItsImports_OnceAndInDependencyOrder()
    {
        var links = IslandHeadLinks.For(Rendered(), Manifest);

        _ = await Assert.That(links.Stylesheets).IsEquivalentTo(
            ["assets/KccSheet-k1.css", "assets/AppHeader-h1.css", "assets/RecipeSearchView-r1.css"],
            CollectionOrdering.Matching);
    }

    [Test]
    public async Task For_NeverRepeatsACssFileTheEntryOrMainCssAlreadyLinks()
    {
        var links = IslandHeadLinks.For(Rendered(), Manifest);

        _ = await Assert.That(links.Stylesheets).DoesNotContain("assets/entry-shared-s1.css");
        _ = await Assert.That(links.Stylesheets).DoesNotContain("assets/main-e1.css");
        _ = await Assert.That(links.Stylesheets).DoesNotContain("assets/Main-a1.css");
    }

    [Test]
    public async Task For_PreloadsEveryIslandChunkAndItsImports_TheSharedVueChunkIncluded()
    {
        var links = IslandHeadLinks.For(Rendered(), Manifest);

        _ = await Assert.That(links.ModulePreloads).IsEquivalentTo(
            ["assets/main-e1.js", "assets/AppHeader-h1.js", "assets/vue-v1.js", "assets/KccSheet-k1.js", "assets/entry-shared-s1.js", "assets/RecipeSearchView-r1.js"],
            CollectionOrdering.Matching);
    }

    [Test]
    public async Task For_PreloadsTheEntryFirst_SoIslandChunksCannotQueueAheadOfIt()
    {
        var links = IslandHeadLinks.For(Rendered(), Manifest);

        _ = await Assert.That(links.ModulePreloads[0]).IsEqualTo("assets/main-e1.js");
        _ = await Assert.That(links.ModulePreloads.Count(file => file == "assets/main-e1.js")).IsEqualTo(1);
    }

    [Test]
    public async Task For_PreloadsOnlyTheEntryForAServerOnlyComponent()
    {
        var result = Rendered() with { Islands = [new IslandEntry("AppFooter", Props(), [])], Components = [Footer] };

        var links = IslandHeadLinks.For(result, Manifest);

        _ = await Assert.That(links.ModulePreloads).IsEquivalentTo(["assets/main-e1.js"], CollectionOrdering.Matching);
        _ = await Assert.That(links.Stylesheets).IsEmpty();
    }

    [Test]
    public async Task For_AResultWithoutHtml_WritesNothing()
    {
        var result = Rendered() with { Html = null };

        var links = IslandHeadLinks.For(result, Manifest);

        _ = await Assert.That(links.Stylesheets).IsEmpty();
        _ = await Assert.That(links.ModulePreloads).IsEmpty();
    }

    // Vite.AspNetCore's own indexer logs a warning for every key it lacks, and most component paths have none.
    [Test]
    public async Task For_ChecksAKeyIsInTheManifestBeforeReadingIt()
    {
        var manifest = new FixtureManifest(Chunks);

        _ = IslandHeadLinks.For(Rendered(), manifest);

        _ = await Assert.That(manifest.Misses).IsEmpty();
    }

    private static JsonElement Props() => JsonDocument.Parse("{}").RootElement;

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
