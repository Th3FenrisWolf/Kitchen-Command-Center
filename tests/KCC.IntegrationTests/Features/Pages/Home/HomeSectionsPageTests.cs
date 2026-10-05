using System.Text.Json;
using System.Text.RegularExpressions;
using KCC.IntegrationTests.Config;

namespace KCC.IntegrationTests.Features.Pages.Home;

public class HomeSectionsPageTests
{
    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    [Test]
    public async Task Home_RendersTheBaselineSectionsInOrder()
    {
        var page = await HomeAsync();

        var blocks = Regex.Matches(page.Body, "data-block=\"([a-zA-Z]+)\"").Select(match => match.Groups[1].Value);

        _ = await Assert.That(string.Join(",", blocks)).IsEqualTo("richTextBlock,cardGridBlock,cardGridBlock,stackerBlock,richTextBlock,richTextBlock");
    }

    [Test]
    [Arguments("<section data-block=\"richTextBlock\">")]
    [Arguments("<section class=\"breakout bg-paper p-6 lg:p-12\" data-block=\"cardGridBlock\">")]
    [Arguments("<section class=\"thin\" data-block=\"stackerBlock\">")]
    public async Task Home_StylesASectionFromItsSettings(string section)
    {
        var page = await HomeAsync();

        _ = await Assert.That(page.Body.Contains(section, StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task Home_SpacesItsSectionsASectionApart()
    {
        var page = await HomeAsync();

        _ = await Assert.That(page.Body.Contains("<div class=\"full-width content-grid mt-12 gap-y-[72px] overflow-x-clip\">", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    [Arguments("<div class=\"kcc-slip kcc-tear-1 mx-auto w-full max-w-2xl\">")]
    [Arguments("<div class=\"kcc-slip kcc-tear-4 w-full lg:sticky lg:top-[calc(2rem_+_var(--pad-offset,0px))] lg:transition-[top]\">")]
    [Arguments("<div class=\"kcc-slip kcc-tear-6 mx-auto w-full max-w-2xl\">")]
    public async Task Home_SetsItsTextOnTornSheets(string sheet)
    {
        var page = await HomeAsync();

        _ = await Assert.That(page.Body.Contains(sheet, StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task Home_KeepsEditorTextOutOfTheVueTemplate()
    {
        var page = await HomeAsync();

        _ = await Assert.That(page.Body.Contains(
            "<div class=\"kcc-body kcc-prose\" v-pre><h1 style=\"text-align: center\">Welcome to Kitchen Command Center!</h1></div>",
            StringComparison.Ordinal)).IsTrue();
        _ = await Assert.That(page.Body.Contains("<div class=\"kcc-secname\"><h2 v-pre>How About Something Sweeter?</h2></div>", StringComparison.Ordinal)).IsTrue();
        _ = await Assert.That(page.Body.Contains("<h2 class=\"kcc-h4\" v-pre>Beef</h2>", StringComparison.Ordinal)).IsTrue();
        _ = await Assert.That(page.Body.Contains("<h3 class=\"kcc-h4\" v-pre>Cake</h3>", StringComparison.Ordinal)).IsTrue();
        _ = await Assert.That(page.Body.Contains("<div class=\"kcc-secname\"><h2 v-pre>Delicious Drinks</h2></div>", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task Home_RunsOneTearCycleDownThePage()
    {
        var page = await HomeAsync();

        var sheetTears = Regex.Matches(page.Body, "<div class=\"kcc-slip kcc-tear-(\\d) ").Select(match => match.Groups[1].Value);
        var gridTears = Regex.Matches(page.Body, "<Card card-color=\"bg-[a-z]+\" :tear=\"(\\d)\">").Select(match => match.Groups[1].Value);
        var stackerTears = page.Prop("cards").EnumerateArray().Select(card => card.GetProperty("tear").GetInt32());

        _ = await Assert.That(string.Join(",", sheetTears)).IsEqualTo("1,4,6,1");
        _ = await Assert.That(string.Join(",", gridTears)).IsEqualTo("2,3,4,5,6,1,2,3");
        _ = await Assert.That(string.Join(",", stackerTears)).IsEqualTo("5,6,1,2,3,5");
        _ = await Assert.That(page.Body.Contains("class=\"grid gap-x-7 gap-y-9 lg:grid-cols-4\"", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task Home_EveryGridCard_OpensToTheRecipes()
    {
        var page = await HomeAsync();

        var links = Regex.Matches(page.Body, "<UnderlineLink href=\"/recipes/\"><span v-pre>View Recipes</span></UnderlineLink>");

        _ = await Assert.That(links.Count).IsEqualTo(8);
    }

    [Test]
    public async Task Home_Stacker_HandsItsCardsToTheComponent()
    {
        var page = await HomeAsync();

        var cards = page.Prop("cards").EnumerateArray().Select(card => (Heading(card), card.GetProperty("backgroundColor").GetString())).ToList();

        _ = await Assert.That(string.Join(",", cards.Select(card => card.Item1))).IsEqualTo("Vodka,Tequila,Gin,Whiskey,Rum,Wine");
        _ = await Assert.That(cards[0].Item2).IsEqualTo("bg-lavender");
    }

    private static string? Heading(JsonElement card) => card.GetProperty("heading").GetString();

    private async Task<RenderedPage> HomeAsync()
    {
        using var client = Site.CreateClient();
        return await RenderedPage.GetAsync(client, "/");
    }
}
