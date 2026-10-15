using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Anthropic.Models.Messages;
using KCC.IntegrationTests.Config;
using KCC.Web.Features.DevTools.RecipeSeed;
using KCC.Web.Features.Pages.VariantDetail;
using KCC.Web.Features.RecipeImport;
using KCC.Web.Features.Recipes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TUnit.Assertions.Enums;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;

namespace KCC.IntegrationTests.Features.RecipeImport;

public class RecipeImportApiTests
{
    private const string ReadPath = "/api/recipe-imports/read";
    private const long StartingTotal = 123_456;

    private const string DocumentedValues =
        """
        {
          "outcome": "recipe",
          "recipe": {
            "name": { "value": "Pancakes", "mark": null },
            "description": { "value": "Fluffy weekend pancakes.",
              "mark": { "kind": "written", "reason": "Not in the photos", "code": null, "arg": null } }
          },
          "variant": {
            "name": { "value": "Buttermilk", "mark": null },
            "description": { "value": "Tangy and tall.",
              "mark": { "kind": "written", "reason": "Not in the photos", "code": null, "arg": null } },
            "prepTime": { "value": 10, "mark": { "kind": "estimate", "reason": "Not printed", "code": null, "arg": null } },
            "cookTime": { "value": 15, "mark": null },
            "servings": { "value": 4, "mark": null }
          },
          "ingredients": [
            { "name": "flour", "quantity": 1.5, "unit": "cups", "isEyeballed": false, "mark": null },
            { "name": "garlic", "quantity": 2, "unit": "cloves", "isEyeballed": false,
              "mark": { "kind": "unclear", "reason": "Printed as 2 to 3", "code": null, "arg": null } },
            { "name": "salt", "quantity": null, "unit": "", "isEyeballed": true, "mark": null }
          ],
          "instructions": [
            { "text": "Whisk the dry ingredients.", "mark": null }
          ],
          "otherRecipes": []
        }
        """;

    private static readonly long Ceiling = (long)(UmbracoSite.ImportCeilingUsd * 1_000_000m);
    private static readonly TimeSpan Patience = TimeSpan.FromSeconds(10);
    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01];
    private static readonly byte[] Text = "Not a photo at all."u8.ToArray();

    // The catalog decides these, so the body is compared with the contract's example without them.
    private static readonly string[] CatalogProperties = ["match", "possibleDuplicate", "recipes"];

    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    private static string ThisMonth => ImportSpendLedger.MonthOf(DateTimeOffset.UtcNow);

    private FakeRecipePhotoReader Reader => Site.PhotoReader;

    private ModelProfile Profile =>
        ModelProfiles.Find(Site.Services.GetRequiredService<IOptionsMonitor<RecipeImportOptions>>().CurrentValue.Model)
        ?? throw new InvalidOperationException("The test site's RecipeImport:Model has no price profile.");

    [After(Test)]
    public void ResetTheReaderAndThisMonthsSpend()
    {
        Reader.Reset();
        TestImportSpend.Set(Site.Services, ThisMonth, 0);
    }

    [Test]
    public async Task WhileTheFakeDelegatesToTheComposersReader_AReadIsUnavailable()
    {
        using var member = await TestMembers.SignedInAsync(Site, "importer");
        TestImportSpend.Set(Site.Services, ThisMonth, StartingTotal);

        using var response = await member.Visitor.PostMultipartAsync(ReadPath, RecipeForm());

        _ = await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.ServiceUnavailable);
        _ = await AssertErrorAsync(response, "unavailable");
        _ = await Assert.That(TestImportSpend.Read(Site.Services, ThisMonth)).IsEqualTo(StartingTotal);
    }

    [Test]
    public async Task WithoutTheAntiforgeryToken_AReadIsABadRequest_AndNothingIsReserved()
    {
        Reader.SwitchOn();
        using var member = await TestMembers.SignedInAsync(Site, "importer");
        TestImportSpend.Set(Site.Services, ThisMonth, StartingTotal);
        using var form = RecipeForm();

        using var response = await member.Visitor.Http.PostAsync(ReadPath, form);

        _ = await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        _ = await Assert.That(Reader.Calls).IsEmpty();
        _ = await Assert.That(TestImportSpend.Read(Site.Services, ThisMonth)).IsEqualTo(StartingTotal);
    }

    [Test]
    public async Task SignedOut_AReadIsUnauthorized_WithAnEmptyBody_AndNothingIsReserved()
    {
        Reader.SwitchOn();
        using var visitor = new MemberClient(Site);
        TestImportSpend.Set(Site.Services, ThisMonth, StartingTotal);

        using var response = await visitor.PostMultipartAsync(ReadPath, RecipeForm());

        _ = await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
        _ = await Assert.That(await response.Content.ReadAsStringAsync()).IsEmpty();
        _ = await Assert.That(Reader.Calls).IsEmpty();
        _ = await Assert.That(TestImportSpend.Read(Site.Services, ThisMonth)).IsEqualTo(StartingTotal);
    }

    [Test]
    public async Task SignedOut_AFormThatCannotBeRead_IsUnauthorized_WithAnEmptyBody()
    {
        Reader.SwitchOn();
        using var visitor = new MemberClient(Site);

        using var response = await visitor.PostMultipartAsync(ReadPath, UnreadableForm());

        _ = await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
        _ = await Assert.That(await response.Content.ReadAsStringAsync()).IsEmpty();
    }

    [Test]
    public async Task AFormThatCannotBeRead_IsInvalidPhotos_AndNothingIsReserved()
    {
        Reader.SwitchOn();
        using var member = await TestMembers.SignedInAsync(Site, "importer");
        TestImportSpend.Set(Site.Services, ThisMonth, StartingTotal);

        using var response = await member.Visitor.PostMultipartAsync(ReadPath, UnreadableForm());

        _ = await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        _ = await AssertErrorAsync(response, "invalidPhotos");
        _ = await Assert.That(Reader.Calls).IsEmpty();
        _ = await Assert.That(TestImportSpend.Read(Site.Services, ThisMonth)).IsEqualTo(StartingTotal);
    }

    [Test]
    public async Task PhotosThatAreNotImages_OrNoPhotosAtAll_AreInvalidPhotos()
    {
        Reader.SwitchOn();
        using var member = await TestMembers.SignedInAsync(Site, "importer");
        TestImportSpend.Set(Site.Services, ThisMonth, StartingTotal);

        using var notAnImage = await member.Visitor.PostMultipartAsync(ReadPath, Form("recipe", null, Text));
        using var noPhotos = await member.Visitor.PostMultipartAsync(ReadPath, Form("recipe", null));

        _ = await Assert.That(notAnImage.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        _ = await AssertErrorAsync(notAnImage, "invalidPhotos");
        _ = await Assert.That(noPhotos.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        _ = await AssertErrorAsync(noPhotos, "invalidPhotos");
        _ = await Assert.That(Reader.Calls).IsEmpty();
        _ = await Assert.That(TestImportSpend.Read(Site.Services, ThisMonth)).IsEqualTo(StartingTotal);
    }

    [Test]
    [Arguments(null, null)]
    [Arguments("banana", null)]
    [Arguments("variant", null)]
    [Arguments("variant", "not-a-guid")]
    public async Task AFlowThatIsNotRecipeOrVariantWithARecipeKey_IsAnInvalidRequest(string? flow, string? recipeKey)
    {
        Reader.SwitchOn();
        using var member = await TestMembers.SignedInAsync(Site, "importer");
        TestImportSpend.Set(Site.Services, ThisMonth, StartingTotal);

        using var response = await member.Visitor.PostMultipartAsync(ReadPath, Form(flow, recipeKey, Jpeg));

        _ = await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        _ = await AssertErrorAsync(response, "invalidRequest");
        _ = await Assert.That(Reader.Calls).IsEmpty();
        _ = await Assert.That(TestImportSpend.Read(Site.Services, ThisMonth)).IsEqualTo(StartingTotal);
    }

    [Test]
    public async Task AnUnpublishedRecipe_InTheVariantFlow_IsNotFound()
    {
        Reader.SwitchOn();
        using var member = await TestMembers.SignedInAsync(Site, "importer");
        var draft = await TestContent.DraftRecipeAsync(Site.Services, "IT Kinkajou Draft Loaf", member.Key);
        TestImportSpend.Set(Site.Services, ThisMonth, StartingTotal);

        using var response = await member.Visitor.PostMultipartAsync(ReadPath, Form("variant", draft.ToString(), Jpeg));

        _ = await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
        var body = await AssertErrorAsync(response, "recipeNotFound");
        _ = await Assert.That(body.GetProperty("error").GetString()).IsEqualTo("Recipe not found.");
        _ = await Assert.That(Reader.Calls).IsEmpty();
        _ = await Assert.That(TestImportSpend.Read(Site.Services, ThisMonth)).IsEqualTo(StartingTotal);
    }

    [Test]
    public async Task ARead_InTheRecipeFlow_AnswersTheDocumentedBody_AndSendsTheReaderNoRecipeKey()
    {
        Reader.Answer(ReadAttempt.Read(Pancakes(), UsageOf(10_000, 2_000)));
        using var member = await TestMembers.SignedInAsync(Site, "importer");

        using var response = await member.Visitor.PostMultipartAsync(ReadPath, RecipeForm());

        _ = await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await AssertTheDocumentedBodyAsync(await response.Content.ReadFromJsonAsync<JsonElement>());
        var sent = Reader.Calls.Single();
        _ = await Assert.That(sent.Flow).IsEqualTo(ImportFlows.Recipe);
        _ = await Assert.That(sent.RecipeKey).IsNull();
        _ = await Assert.That(sent.Photos.Single().MediaType).IsEqualTo("image/jpeg");
        _ = await Assert.That(sent.Photos.Single().Data.SequenceEqual(Jpeg)).IsTrue();
    }

    [Test]
    public async Task ARead_InTheVariantFlow_AnswersTheDocumentedBody_AndSendsTheReaderTheChosenRecipesKey()
    {
        Reader.Answer(ReadAttempt.Read(Pancakes(), UsageOf(10_000, 2_000)));
        var recipe = await TestContent.RecipeAsync(Site.Services, "IT Kinkajou Griddle Cakes");
        using var member = await TestMembers.SignedInAsync(Site, "importer");
        using var form = Form("variant", recipe.ToString(), Jpeg, Jpeg);

        using var response = await member.Visitor.PostMultipartAsync(ReadPath, form);

        _ = await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        await AssertTheDocumentedBodyAsync(body);
        _ = await Assert.That(body.GetProperty("match").ValueKind).IsEqualTo(JsonValueKind.Null);
        _ = await Assert.That(body.GetProperty("recipes").ValueKind).IsEqualTo(JsonValueKind.Null);
        var sent = Reader.Calls.Single();
        _ = await Assert.That(sent.Flow).IsEqualTo(ImportFlows.Variant);
        _ = await Assert.That(sent.RecipeKey).IsEqualTo(recipe);
        _ = await Assert.That(sent.Photos.Count).IsEqualTo(2);
    }

    [Test]
    public async Task AMatchIdOfR1_GivesTheMatchWithThatRecipesKeyAndName_AndTheRecipesList()
    {
        Reader.Answer(ReadAttempt.Read(Pancakes() with { MatchId = "R1", MatchReason = "Same dish." }, UsageOf(10_000, 2_000)));
        using var member = await TestMembers.SignedInAsync(Site, "importer");

        using var response = await member.Visitor.PostMultipartAsync(ReadPath, RecipeForm());

        _ = await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var catalog = PublishedCatalog();
        var first = catalog.Find("R1")!;
        var match = body.GetProperty("match");
        _ = await Assert.That(match.GetProperty("recipeKey").GetGuid()).IsEqualTo(first.Key);
        _ = await Assert.That(match.GetProperty("recipeName").GetString()).IsEqualTo(first.Name);
        _ = await Assert.That(match.GetProperty("reason").GetString()).IsEqualTo("Same dish.");
        _ = await Assert.That(Choices(body)).IsEquivalentTo(
            catalog.Choices.Select(choice => (choice.Key, (string?)choice.Name)),
            CollectionOrdering.Matching);
        _ = await Assert.That(Reader.Calls.Single().Catalog?.Text).IsEqualTo(catalog.Text);
    }

    [Test]
    public async Task AnUnknownMatchId_GivesNoMatch_ButTheRecipesList()
    {
        Reader.Answer(ReadAttempt.Read(Pancakes() with { MatchId = "R99999", MatchReason = "Same dish." }, UsageOf(10_000, 2_000)));
        using var member = await TestMembers.SignedInAsync(Site, "importer");

        using var response = await member.Visitor.PostMultipartAsync(ReadPath, RecipeForm());

        _ = await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        _ = await Assert.That(body.GetProperty("match").ValueKind).IsEqualTo(JsonValueKind.Null);
        _ = await Assert.That(Choices(body).Count).IsEqualTo(PublishedCatalog().Choices.Count);
    }

    [Test]
    public async Task TheVariantFlow_GivesNeitherAMatchNorTheRecipesList_AndSendsTheReaderNoCatalog()
    {
        Reader.Answer(ReadAttempt.Read(Pancakes() with { MatchId = "R1", MatchReason = "Same dish." }, UsageOf(10_000, 2_000)));
        using var member = await TestMembers.SignedInAsync(Site, "importer");
        var ramen = SeedKeys.Recipe("Spicy Ramen Flight");

        using var response = await member.Visitor.PostMultipartAsync(ReadPath, Form("variant", ramen.ToString(), Jpeg));

        _ = await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        _ = await Assert.That(body.GetProperty("match").ValueKind).IsEqualTo(JsonValueKind.Null);
        _ = await Assert.That(body.GetProperty("recipes").ValueKind).IsEqualTo(JsonValueKind.Null);
        _ = await Assert.That(Reader.Calls.Single().Catalog).IsNull();
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task ARecipeFlowRead_ThatCopiesASeededVariant_GivesItAsAPossibleDuplicate_WhicheverRecipeItProposes(bool proposesAnother)
    {
        var ramen = PublishedRecipe("Spicy Ramen Flight");
        var shoyu = ramen.Variants.Single(variant => variant.Name == "Chili Oil Shoyu");
        var another = PublishedCatalog().Entries.First(entry => entry.Key != ramen.Recipe.Key).ShortId;
        Reader.Answer(ReadAttempt.Read(CopyOf(shoyu) with { MatchId = proposesAnother ? another : null }, UsageOf(10_000, 2_000)));
        using var member = await TestMembers.SignedInAsync(Site, "importer");

        using var response = await member.Visitor.PostMultipartAsync(ReadPath, RecipeForm());

        _ = await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        _ = await Assert.That(PossibleDuplicateOf(body)).IsEqualTo((shoyu.Key, "Chili Oil Shoyu", "Spicy Ramen Flight"));
    }

    [Test]
    public async Task AVariantFlowRead_ThatCopiesAVariantOfTheChosenRecipe_GivesItAsAPossibleDuplicate()
    {
        var ramen = PublishedRecipe("Spicy Ramen Flight");
        var shoyu = ramen.Variants.Single(variant => variant.Name == "Chili Oil Shoyu");
        Reader.Answer(ReadAttempt.Read(CopyOf(shoyu), UsageOf(10_000, 2_000)));
        using var member = await TestMembers.SignedInAsync(Site, "importer");

        using var response = await member.Visitor.PostMultipartAsync(ReadPath, Form("variant", ramen.Recipe.Key.ToString(), Jpeg));

        _ = await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        _ = await Assert.That(PossibleDuplicateOf(body)).IsEqualTo((shoyu.Key, "Chili Oil Shoyu", "Spicy Ramen Flight"));
    }

    [Test]
    public async Task AVariantFlowRead_ThatCopiesAnotherRecipesVariant_GivesNoPossibleDuplicate()
    {
        var shoyu = PublishedRecipe("Spicy Ramen Flight").Variants.Single(variant => variant.Name == "Chili Oil Shoyu");
        var cake = SeedKeys.Recipe("Molten Chocolate Cake");
        Reader.Answer(ReadAttempt.Read(CopyOf(shoyu), UsageOf(10_000, 2_000)));
        using var member = await TestMembers.SignedInAsync(Site, "importer");

        using var response = await member.Visitor.PostMultipartAsync(ReadPath, Form("variant", cake.ToString(), Jpeg));

        _ = await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        _ = await Assert.That(body.GetProperty("possibleDuplicate").ValueKind).IsEqualTo(JsonValueKind.Null);
    }

    [Test]
    public async Task AReadThatFindsNoRecipe_AnswersTheNoRecipeOutcome_WithEveryValueEmpty()
    {
        Reader.Answer(ReadAttempt.Read(Pancakes() with { Found = false }, UsageOf(8_000, 300)));
        using var member = await TestMembers.SignedInAsync(Site, "importer");

        using var response = await member.Visitor.PostMultipartAsync(ReadPath, RecipeForm());

        _ = await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        _ = await Assert.That(body.GetProperty("outcome").GetString()).IsEqualTo("noRecipe");
        _ = await Assert.That(body.GetProperty("recipe").GetProperty("name").GetProperty("value").GetString()).IsEmpty();
        var prepTime = body.GetProperty("variant").GetProperty("prepTime").GetProperty("value");
        _ = await Assert.That(prepTime.ValueKind).IsEqualTo(JsonValueKind.Null);
        _ = await Assert.That(body.GetProperty("ingredients").GetArrayLength()).IsEqualTo(0);
        _ = await Assert.That(body.GetProperty("instructions").GetArrayLength()).IsEqualTo(0);
        _ = await Assert.That(body.GetProperty("recipes").ValueKind).IsEqualTo(JsonValueKind.Null);
    }

    [Test]
    public async Task ARecipeFlowRead_KeepsADraftRecipeAndADraftVariant_OutOfTheCatalogAndTheRecipesList()
    {
        Reader.Answer(ReadAttempt.Read(Pancakes(), UsageOf(10_000, 2_000)));
        using var member = await TestMembers.SignedInAsync(Site, "importer");
        var parent = await TestContent.RecipeAsync(Site.Services, "IT Catalog Parent");
        _ = await TestContent.DraftVariantAsync(Site.Services, parent, "IT Draft Only Variant", member.Key);
        _ = await TestContent.DraftRecipeAsync(Site.Services, "IT Draft Only Recipe", member.Key);

        using var response = await member.Visitor.PostMultipartAsync(ReadPath, RecipeForm());

        _ = await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var catalogText = Reader.Calls.Single().Catalog?.Text ?? string.Empty;
        var listed = Choices(await response.Content.ReadFromJsonAsync<JsonElement>()).Select(choice => choice.Name).ToList();
        _ = await Assert.That(catalogText).Contains("IT Catalog Parent");
        _ = await Assert.That(listed).Contains("IT Catalog Parent");
        foreach (var draft in new[] { "IT Draft Only Variant", "IT Draft Only Recipe" })
        {
            _ = await Assert.That(catalogText).DoesNotContain(draft);
            _ = await Assert.That(listed).DoesNotContain(draft);
        }
    }

    [Test]
    public async Task AVariantFlowRead_ThatGetsNoAnswer_KeepsAReservationThatPricesNoCatalog()
    {
        Reader.Answer(ReadAttempt.NoAnswer());
        using var member = await TestMembers.SignedInAsync(Site, "importer");
        var ramen = SeedKeys.Recipe("Spicy Ramen Flight");
        TestImportSpend.Set(Site.Services, ThisMonth, StartingTotal);

        using var response = await member.Visitor.PostMultipartAsync(ReadPath, Form("variant", ramen.ToString(), Jpeg));

        _ = await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadGateway);
        var reserved = ReadCost.ReservationMicroUsd(Profile, 1, 0);
        _ = await Assert.That(TestImportSpend.Read(Site.Services, ThisMonth)).IsEqualTo(StartingTotal + reserved);
    }

    [Test]
    public async Task ARead_ChangesTheMonthsTotalByItsActualCost()
    {
        var usage = UsageOf(10_000, 2_000);
        Reader.Answer(ReadAttempt.Read(Pancakes(), usage));
        using var member = await TestMembers.SignedInAsync(Site, "importer");
        TestImportSpend.Set(Site.Services, ThisMonth, StartingTotal);

        using var response = await member.Visitor.PostMultipartAsync(ReadPath, RecipeForm());

        _ = await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        _ = await Assert.That(TestImportSpend.Read(Site.Services, ThisMonth)).IsEqualTo(StartingTotal + CostOf(usage));
    }

    [Test]
    [Arguments(ReadFailure.Refused)]
    [Arguments(ReadFailure.CutOff)]
    [Arguments(ReadFailure.Unusable)]
    public async Task AnAnswerWithUsageThatIsNoRead_IsReadFailed_AndSettlesItsActualCost(ReadFailure failure)
    {
        var usage = UsageOf(9_000, 12_000);
        Reader.Answer(failure switch
        {
            ReadFailure.Refused => ReadAttempt.Refused(usage),
            ReadFailure.CutOff => ReadAttempt.CutOff(usage),
            _ => ReadAttempt.Unusable(usage),
        });
        using var member = await TestMembers.SignedInAsync(Site, "importer");
        TestImportSpend.Set(Site.Services, ThisMonth, StartingTotal);

        using var response = await member.Visitor.PostMultipartAsync(ReadPath, RecipeForm());

        _ = await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadGateway);
        _ = await AssertErrorAsync(response, "readFailed");
        _ = await Assert.That(TestImportSpend.Read(Site.Services, ThisMonth)).IsEqualTo(StartingTotal + CostOf(usage));
    }

    [Test]
    public async Task AnErrorStatusFromTheService_IsReadFailed_AndLeavesTheMonthsTotalUnchanged()
    {
        Reader.Answer(ReadAttempt.ServiceError());
        using var member = await TestMembers.SignedInAsync(Site, "importer");
        TestImportSpend.Set(Site.Services, ThisMonth, StartingTotal);

        using var response = await member.Visitor.PostMultipartAsync(ReadPath, RecipeForm());

        _ = await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadGateway);
        _ = await AssertErrorAsync(response, "readFailed");
        _ = await Assert.That(TestImportSpend.Read(Site.Services, ThisMonth)).IsEqualTo(StartingTotal);
    }

    [Test]
    public async Task AFailedConnection_IsReadFailed_AndKeepsTheReservation()
    {
        Reader.Answer(ReadAttempt.NoAnswer());
        using var member = await TestMembers.SignedInAsync(Site, "importer");
        TestImportSpend.Set(Site.Services, ThisMonth, StartingTotal);

        using var response = await member.Visitor.PostMultipartAsync(ReadPath, Form("recipe", null, Jpeg, Jpeg, Jpeg));

        _ = await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadGateway);
        _ = await AssertErrorAsync(response, "readFailed");
        var reserved = ReservationFor(photos: 3);
        _ = await Assert.That(TestImportSpend.Read(Site.Services, ThisMonth)).IsEqualTo(StartingTotal + reserved);
    }

    [Test]
    public async Task TheReadDeadline_IsReadTimedOut_AndKeepsTheReservation()
    {
        Reader.Answer(ReadAttempt.TimedOut());
        using var member = await TestMembers.SignedInAsync(Site, "importer");
        TestImportSpend.Set(Site.Services, ThisMonth, StartingTotal);

        using var response = await member.Visitor.PostMultipartAsync(ReadPath, RecipeForm());

        _ = await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.GatewayTimeout);
        _ = await AssertErrorAsync(response, "readTimedOut");
        var reserved = ReservationFor(photos: 1);
        _ = await Assert.That(TestImportSpend.Read(Site.Services, ThisMonth)).IsEqualTo(StartingTotal + reserved);
    }

    [Test]
    public async Task AtTheCeiling_AReadIsRefusedWithTheDayItIsAvailableAgain_AndTheReaderIsNotCalled()
    {
        Reader.SwitchOn();
        using var member = await TestMembers.SignedInAsync(Site, "importer");
        TestImportSpend.Set(Site.Services, ThisMonth, Ceiling);
        var today = DateTime.UtcNow;

        using var response = await member.Visitor.PostMultipartAsync(ReadPath, RecipeForm());

        _ = await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Conflict);
        var body = await AssertErrorAsync(response, "ceilingReached");
        var firstOfNextMonth = new DateOnly(today.Year, today.Month, 1).AddMonths(1);
        _ = await Assert.That(body.GetProperty("availableAgainOn").GetString())
            .IsEqualTo(firstOfNextMonth.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        _ = await Assert.That(Reader.Calls).IsEmpty();
        _ = await Assert.That(TestImportSpend.Read(Site.Services, ThisMonth)).IsEqualTo(Ceiling);
    }

    [Test]
    public async Task AReadWhoseClientGoesAwayMidRead_RunsToItsEnd_AndSettlesItsActualCost()
    {
        var usage = UsageOf(12_000, 3_000);
        var held = Reader.HoldOpen();
        using var member = await TestMembers.SignedInAsync(Site, "importer");
        TestImportSpend.Set(Site.Services, ThisMonth, StartingTotal);
        using var leaving = new CancellationTokenSource();

        var request = member.Visitor.PostMultipartAsync(ReadPath, RecipeForm(), leaving.Token);
        await held.Started.WaitAsync(Patience);
        await leaving.CancelAsync();
        held.Release(ReadAttempt.Read(Pancakes(), usage));

        _ = await Assert.That(async () => await request.WaitAsync(Patience)).Throws<OperationCanceledException>();
        var settled = StartingTotal + CostOf(usage);
        _ = await Assert.That(await TotalOnceItReachesAsync(settled)).IsEqualTo(settled);
    }

    [Test]
    public async Task ARead_CreatesNoMediaItem()
    {
        Reader.Answer(ReadAttempt.Read(Pancakes(), UsageOf(10_000, 2_000)));
        using var member = await TestMembers.SignedInAsync(Site, "importer");
        var before = MediaCount();

        using var response = await member.Visitor.PostMultipartAsync(ReadPath, Form("recipe", null, Jpeg, Jpeg));

        _ = await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        _ = await Assert.That(MediaCount()).IsEqualTo(before);
    }

    [Test]
    public async Task TheEleventhReadInAnHour_IsTooManyRequests()
    {
        Reader.Answer(ReadAttempt.Read(Pancakes(), UsageOf(1_000, 100)));
        using var member = await TestMembers.SignedInAsync(Site, "importer");
        for (var read = 0; read < 10; read++)
        {
            using var allowed = await member.Visitor.PostMultipartAsync(ReadPath, RecipeForm());
            _ = await Assert.That(allowed.StatusCode).IsEqualTo(HttpStatusCode.OK);
        }

        using var limited = await member.Visitor.PostMultipartAsync(ReadPath, RecipeForm());

        _ = await Assert.That(limited.StatusCode).IsEqualTo(HttpStatusCode.TooManyRequests);
        _ = await Assert.That(limited.Headers.RetryAfter?.Delta).IsNotNull();
        _ = await Assert.That(Reader.Calls.Count).IsEqualTo(10);
    }

    private static MultipartFormDataContent RecipeForm() => Form("recipe", null, Jpeg);

    private static MultipartFormDataContent UnreadableForm()
    {
        var form = RecipeForm();
        form.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/form-data; boundary=another-boundary");
        return form;
    }

    private static MultipartFormDataContent Form(string? flow, string? recipeKey, params byte[][] photos)
    {
        var form = new MultipartFormDataContent();
        for (var index = 0; index < photos.Length; index++)
        {
            var photo = new ByteArrayContent(photos[index]);
            photo.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
            form.Add(photo, "photos", $"photo-{index + 1}.jpg");
        }

        if (flow is not null)
        {
            form.Add(new StringContent(flow), "flow");
        }

        if (recipeKey is not null)
        {
            form.Add(new StringContent(recipeKey), "recipeKey");
        }

        return form;
    }

    private static ReaderOutput Pancakes() => new(
        Found: true,
        new ReaderRecipe(
            new ReaderText("Pancakes", null),
            new ReaderText("Fluffy weekend pancakes.", new ReaderMark("written", "Not in the photos"))),
        new ReaderVariant(
            new ReaderText("Buttermilk", null),
            new ReaderText("Tangy and tall.", new ReaderMark("written", "Not in the photos")),
            new ReaderCount(10, new ReaderMark("estimate", "Not printed")),
            new ReaderCount(15, null),
            new ReaderCount(4, null)),
        [
            new ReaderIngredient("flour", 1.5m, "cups", false, null),
            new ReaderIngredient("garlic", 2, "cloves", false, new ReaderMark("unclear", "Printed as 2 to 3")),
            new ReaderIngredient("salt", null, string.Empty, true, null),
        ],
        [new ReaderStep("Whisk the dry ingredients.", null)],
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

    private static async Task<JsonElement> AssertErrorAsync(HttpResponseMessage response, string code)
    {
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        _ = await Assert.That(body.GetProperty("code").GetString()).IsEqualTo(code);
        _ = await Assert.That(string.IsNullOrWhiteSpace(body.GetProperty("error").GetString())).IsFalse();
        return body;
    }

    private static async Task AssertTheDocumentedBodyAsync(JsonElement body)
    {
        var values = JsonNode.Parse(body.GetRawText())!.AsObject();
        foreach (var property in CatalogProperties)
        {
            _ = await Assert.That(values.ContainsKey(property)).IsTrue();
            _ = values.Remove(property);
        }

        _ = await Assert.That(values.ToJsonString()).IsEqualTo(JsonNode.Parse(DocumentedValues)!.ToJsonString());
    }

    private long CostOf(Usage usage) => ReadCost.MicroUsd(Profile, usage);

    private long ReservationFor(int photos) =>
        ReadCost.ReservationMicroUsd(Profile, photos, catalogCharacters: PublishedCatalog().Characters);

    private static List<(Guid Key, string? Name)> Choices(JsonElement body) =>
        [.. body.GetProperty("recipes").EnumerateArray().Select(choice => (choice.GetProperty("key").GetGuid(), choice.GetProperty("name").GetString()))];

    private static (Guid, string?, string?) PossibleDuplicateOf(JsonElement body)
    {
        var duplicate = body.GetProperty("possibleDuplicate");
        return duplicate.ValueKind == JsonValueKind.Null
            ? default
            : (duplicate.GetProperty("variantKey").GetGuid(),
                duplicate.GetProperty("variantName").GetString(),
                duplicate.GetProperty("recipeName").GetString());
    }

    private static ReaderOutput CopyOf(VariantRecord variant) => Pancakes() with
    {
        Ingredients =
        [
            .. KCC.Web.Features.Helpers.JsonSerializer.DeserializeCollection<IngredientViewModel>(variant.IngredientsJson)
                .Select(row => new ReaderIngredient(row.Name, row.Quantity, row.Unit, row.IsEyeballed, null)),
        ],
        Steps =
        [
            .. KCC.Web.Features.Helpers.JsonSerializer.DeserializeCollection<InstructionViewModel>(variant.InstructionsJson)
                .Select(row => new ReaderStep(row.Text, null)),
        ],
    };

    private RecipeCatalog PublishedCatalog() => WithRecipes(queries => RecipeCatalog.From(queries.GetPublishedRecipes()));

    private RecipePageData PublishedRecipe(string name) =>
        WithRecipes(queries => queries.GetPublishedRecipes().Single(page => page.Recipe.Name == name));

    private T WithRecipes<T>(Func<IRecipeQueries, T> read)
    {
        using var context = Site.Services.GetRequiredService<IUmbracoContextFactory>().EnsureUmbracoContext();
        using var scope = Site.Services.CreateScope();
        return read(scope.ServiceProvider.GetRequiredService<IRecipeQueries>());
    }

    private async Task<long?> TotalOnceItReachesAsync(long expected)
    {
        for (var deadline = DateTime.UtcNow + Patience; DateTime.UtcNow < deadline; await Task.Delay(50))
        {
            if (TestImportSpend.Read(Site.Services, ThisMonth) == expected)
            {
                return expected;
            }
        }

        return TestImportSpend.Read(Site.Services, ThisMonth);
    }

    private int MediaCount()
    {
        using var scope = Site.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IMediaService>().Count();
    }
}
