using System.Text.Json;
using KCC.IntegrationTests.Config;
using KCC.Tests;

namespace KCC.IntegrationTests.Features.Pages;

// The test host renders with SSR off, so every page here is the page a sidecar failure serves.
public class FallbackPageTests
{
    private const string FallbackEntry = "Features/Fallback.ts";
    private const string IslandEntry = "Features/Main.ts";

    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    public static IEnumerable<string> Paths() => ["/", "/recipes/", "/recipes/it-rated-recipe/", "/account/login/"];

    [Test]
    [MethodDataSource(nameof(Paths))]
    public async Task APageWithoutServerHtml_LinksTheFallbackEntry_AndNotTheIslandEntry(string path)
    {
        using var client = Site.CreateClient();
        var page = await RenderedPage.GetAsync(client, path);
        var fallback = Manifest("fallback");
        var main = Manifest(string.Empty);

        _ = await Assert.That(page.Html).Contains($"<script type=\"module\" src=\"/fallback/{fallback.GetProperty(FallbackEntry).GetProperty("file").GetString()}\"></script>");
        _ = await Assert.That(page.Html).DoesNotContain(IslandEntry);
        _ = await Assert.That(page.Html).DoesNotContain(main.GetProperty(IslandEntry).GetProperty("file").GetString()!);
    }

    // Vite.AspNetCore reads only the main manifest, so these links are all that carries the components' style blocks.
    [Test]
    [MethodDataSource(nameof(Paths))]
    public async Task APageWithoutServerHtml_LinksEveryStylesheetOfTheFallbackEntrysClosure(string path)
    {
        using var client = Site.CreateClient();
        var page = await RenderedPage.GetAsync(client, path);
        var stylesheets = CssClosure(Manifest("fallback"), FallbackEntry);

        _ = await Assert.That(stylesheets).IsNotEmpty();
        foreach (var css in stylesheets)
        {
            _ = await Assert.That(page.Html).Contains($"<link rel=\"stylesheet\" href=\"/fallback/{css}\" />");
        }
    }

    [Test]
    [MethodDataSource(nameof(Paths))]
    public async Task APageWithoutServerHtml_CarriesTheThreeRegionTemplates(string path)
    {
        using var client = Site.CreateClient();
        var page = await RenderedPage.GetAsync(client, path);

        _ = await Assert.That(page.Header).IsNotEmpty();
        _ = await Assert.That(page.Body).IsNotEmpty();
        _ = await Assert.That(page.Html).Contains("\"footerContent\":\"");
    }

    private static JsonElement Manifest(string folder)
    {
        var path = Path.Combine(RepoPaths.WebProject, "wwwroot", folder, ".vite", "manifest.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        return document.RootElement.Clone();
    }

    private static List<string> CssClosure(JsonElement manifest, string entry)
    {
        var css = new List<string>();
        var visited = new HashSet<string>();
        Collect(entry);
        return css;

        void Collect(string key)
        {
            if (!visited.Add(key) || !manifest.TryGetProperty(key, out var chunk))
            {
                return;
            }

            foreach (var import in Strings(chunk, "imports"))
            {
                Collect(import);
            }

            foreach (var file in Strings(chunk, "css").Where(file => !css.Contains(file)))
            {
                css.Add(file);
            }
        }
    }

    private static IEnumerable<string> Strings(JsonElement chunk, string property) =>
        chunk.TryGetProperty(property, out var values) ? values.EnumerateArray().Select(value => value.GetString()!) : [];
}
