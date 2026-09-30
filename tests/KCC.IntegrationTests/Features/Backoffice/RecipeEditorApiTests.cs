using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using KCC.Admin;
using KCC.IntegrationTests.Config;

namespace KCC.IntegrationTests.Features.Backoffice;

public class RecipeEditorApiTests
{
    private const string Base = "/umbraco/management/api/v1/kcc/recipe-editor";

    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    [Test]
    public async Task Icons_AreTheCuratedList()
    {
        using var editor = await BackofficeClient.EditorAsync(Site);

        var icons = await editor.GetJsonAsync($"{Base}/icons");

        _ = await Assert.That(icons.EnumerateArray().Select(icon => icon.GetString()!).ToList()).IsEquivalentTo(RecipeIcons.All);
    }

    [Test]
    public async Task Units_AreTheCuratedList()
    {
        using var editor = await BackofficeClient.EditorAsync(Site);

        var units = await editor.GetJsonAsync($"{Base}/units");

        _ = await Assert.That(units.EnumerateArray().Select(unit => unit.GetString()!).ToList()).IsEquivalentTo(RecipeUnits.All);
    }

    [Test]
    public async Task IconSuggestion_WithoutAnAnthropicKey_IsTheNamesFallback()
    {
        using var editor = await BackofficeClient.EditorAsync(Site);

        using var response = await editor.PostAsync($"{Base}/icon-suggestion", new { name = "Garnet Tacos", description = "Crisp shells." });
        var suggestion = await response.Content.ReadFromJsonAsync<JsonElement>();

        _ = await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        _ = await Assert.That(suggestion.GetProperty("icon").GetString()).IsEqualTo(RecipeIcons.Fallback("Garnet Tacos"));
    }

    [Test]
    public async Task Endpoints_WithoutABackofficeToken_AreUnauthorized()
    {
        using var anonymous = Site.CreateClient();

        using var icons = await anonymous.GetAsync($"{Base}/icons");
        using var suggestion = await anonymous.PostAsJsonAsync($"{Base}/icon-suggestion", new { name = "Obsidian Pie", description = string.Empty });

        _ = await Assert.That(icons.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
        _ = await Assert.That(suggestion.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Endpoints_WithoutContentAccess_AreForbidden()
    {
        using var translator = await BackofficeClient.TranslatorAsync(Site);

        using var icons = await translator.GetAsync($"{Base}/icons");
        using var units = await translator.GetAsync($"{Base}/units");
        using var suggestion = await translator.PostAsync($"{Base}/icon-suggestion", new { name = "Jasper Stew", description = string.Empty });

        _ = await Assert.That(icons.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
        _ = await Assert.That(units.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
        _ = await Assert.That(suggestion.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
    }
}
