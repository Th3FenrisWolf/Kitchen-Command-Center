using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Anthropic.Models.Messages;
using KCC.IntegrationTests.Config;
using KCC.Web.Features.RecipeImport;

namespace KCC.IntegrationTests.Features.RecipeImport;

public class RecipeImportCeilingTests
{
    private const string ReadPath = "/api/recipe-imports/read";

    private static readonly long Ceiling = (long)(UmbracoSite.ImportCeilingUsd * 1_000_000m);
    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01];

    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    private static string ThisMonth => ImportSpendLedger.MonthOf(DateTimeOffset.UtcNow);

    [After(Test)]
    public void ResetTheReaderAndThisMonthsSpend()
    {
        Site.PhotoReader.Reset();
        TestImportSpend.Set(Site.Services, ThisMonth, 0);
    }

    [Test]
    public async Task AReadThatReachesTheCeiling_LeavesTheImportUnavailableUntilNextMonth_AndTypingStillWorks()
    {
        Site.PhotoReader.Answer(ReadAttempt.Read(Waffles(), UsageOf(10_000, 2_000)));
        var recipe = await TestContent.RecipeAsync(Site.Services, "IT Kinkajou Ceiling Waffles");
        using var member = await TestMembers.SignedInAsync(Site, "importer");
        TestImportSpend.Set(Site.Services, ThisMonth, Ceiling - 1);
        var availableAgainOn = ImportAvailability.AvailableAgainOn(DateTimeOffset.UtcNow)
            .ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        using var lastRead = await member.Visitor.PostMultipartAsync(ReadPath, RecipeForm());
        using var refused = await member.Visitor.PostMultipartAsync(ReadPath, RecipeForm());
        var create = await RenderedPage.GetAsync(member.Visitor.Http, "/recipes/create-recipe/");
        var add = await RenderedPage.GetAsync(member.Visitor.Http, $"/recipes/add-variant/?recipe={recipe}");
        using var typed = await member.Visitor.PostAsync("/api/recipes", TypedRecipe("IT Kinkajou Typed Waffles"));

        _ = await Assert.That(lastRead.StatusCode).IsEqualTo(HttpStatusCode.OK);
        _ = await Assert.That(refused.StatusCode).IsEqualTo(HttpStatusCode.Conflict);
        var body = await refused.Content.ReadFromJsonAsync<JsonElement>();
        _ = await Assert.That(body.GetProperty("code").GetString()).IsEqualTo("ceilingReached");
        _ = await Assert.That(body.GetProperty("availableAgainOn").GetString()).IsEqualTo(availableAgainOn);
        _ = await Assert.That(Site.PhotoReader.Calls.Count).IsEqualTo(1);
        foreach (var import in new[] { create.Prop("photo-import"), add.Prop("photo-import") })
        {
            _ = await Assert.That(import.GetProperty("enabled").GetBoolean()).IsTrue();
            _ = await Assert.That(import.GetProperty("available").GetBoolean()).IsFalse();
            _ = await Assert.That(import.GetProperty("availableAgainOn").GetString()).IsEqualTo(availableAgainOn);
        }

        _ = await Assert.That(typed.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    private static MultipartFormDataContent RecipeForm()
    {
        var photo = new ByteArrayContent(Jpeg);
        photo.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        return new MultipartFormDataContent
        {
            { photo, "photos", "photo-1.jpg" },
            { new StringContent("recipe"), "flow" },
        };
    }

    private static object TypedRecipe(string name) => new
    {
        recipeName = name,
        recipeDescription = "Typed in while the import rests.",
        firstVariant = new
        {
            variantName = "Crisp",
            variantDescription = "Golden.",
            prepTime = 10,
            cookTime = 5,
            servings = 4,
            ingredients = new[] { new { name = "Flour", quantity = 2, unit = "Cups", isEyeballed = false } },
            instructions = new[] { new { step = 1, text = "Cook in a hot iron." } },
        },
    };

    private static ReaderOutput Waffles() => new(
        Found: true,
        new ReaderRecipe(new ReaderText("Waffles", null), new ReaderText("Crisp waffles.", null)),
        new ReaderVariant(
            new ReaderText("Classic", null),
            new ReaderText("Golden.", null),
            new ReaderCount(10, null),
            new ReaderCount(5, null),
            new ReaderCount(4, null)),
        [new ReaderIngredient("flour", 2, "cups", false, null)],
        [new ReaderStep("Cook in a hot iron.", null)],
        MatchId: null,
        MatchReason: null,
        OtherRecipes: [],
        SourceLanguage: null);

    private static Usage UsageOf(long input, long output) => new()
    {
        InputTokens = input,
        OutputTokens = output,
        CacheCreationInputTokens = null,
        CacheReadInputTokens = null,
        CacheCreation = null,
        InferenceGeo = null,
        OutputTokensDetails = null,
        ServerToolUse = null,
        ServiceTier = null,
    };
}
