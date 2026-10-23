using System.Globalization;
using System.Text.RegularExpressions;
using KCC.E2ETests.Config;
using Microsoft.Playwright;

namespace KCC.E2ETests.Features.Chrome;

public partial class BloomArrangementTests : BasePageTests
{
    private const string Library = "/recipes/";

    private static readonly string[] BrandWashes = ["peach", "yellow", "green", "teal", "sky", "lavender", "pink", "red"];

    [Test]
    public async Task OnTheLibrary_NoTwoSheetsShareAnArrangement_AndNoneHoldsMoreThanFour()
    {
        await RecipeIndex.CurrentAsync(Page);
        await Visit.BeginAsync(Page.Context, Site.BaseUrl, 300);
        await Page.GotoAsync(Library);
        await Washes.SettleAsync(Page);

        var fields = await Washes.ReadAsync(Page);
        var arrangements = fields.Select(field => string.Join(" / ", field.Washes.Select(Variety))).ToArray();

        _ = await Assert.That(fields.Length).IsGreaterThanOrEqualTo(6);
        _ = await Assert.That(fields.Where(field => field.Washes.Length > 4).Select(field => field.Washes.Length)).IsEmpty();
        _ = await Assert.That(arrangements.GroupBy(arrangement => arrangement).Where(group => group.Count() > 1).Select(group => group.Key)).IsEmpty();
    }

    [Test]
    [Arguments("light")]
    [Arguments("dark")]
    public async Task EveryWashesInk_IsABrandWashColourExactly(string ramp)
    {
        await RecipeIndex.CurrentAsync(Page);
        await Page.EmulateMediaAsync(new() { ColorScheme = ramp == "dark" ? ColorScheme.Dark : ColorScheme.Light });
        await Visit.BeginAsync(Page.Context, Site.BaseUrl, 300);
        await Page.GotoAsync(Library);
        await Washes.SettleAsync(Page);
        await Expect(Page.Locator("html")).ToHaveAttributeAsync("data-theme", ramp);

        var read = await Page.EvaluateAsync<InkReading>(
            """
            (names) => {
              const colourOf = (parent, value) => {
                const probe = document.createElement('span')
                probe.style.color = value
                parent.append(probe)
                const colour = getComputedStyle(probe).color
                probe.remove()
                return colour
              }
              const brand = names.map((name) => colourOf(document.body, `var(--color-${name})`))
              const inks = [...document.querySelectorAll('.kcc-wash')]
                .filter((wash) => wash.checkVisibility())
                .flatMap((wash) => [colourOf(wash, 'var(--wash-ink)'), colourOf(wash, 'var(--wash-ink-2)')])
              return { brand, inks }
            }
            """,
            BrandWashes);

        _ = await Assert.That(read.Brand.Distinct().Count()).IsEqualTo(BrandWashes.Length);
        _ = await Assert.That(read.Inks).IsNotEmpty();
        _ = await Assert.That(read.Inks.Where(ink => !read.Brand.Contains(ink))).IsEmpty();
    }

    [Test]
    public async Task NeighbouringSheets_DoNotBloomInStep()
    {
        await RecipeIndex.CurrentAsync(Page);
        await Visit.BeginAsync(Page.Context, Site.BaseUrl, 300);
        await Page.GotoAsync(Library);
        await Washes.SettleAsync(Page);

        var fields = await Washes.ReadAsync(Page);
        var starts = fields.Select(field => field.Washes.Select(wash => wash.At).ToHashSet()).ToArray();
        var inStep = Enumerable.Range(1, starts.Length - 1)
            .Where(i => starts[i - 1].Overlaps(starts[i]))
            .Select(i => string.Join(", ", starts[i - 1].Intersect(starts[i]).Select(at => at.ToString(CultureInfo.InvariantCulture))));

        _ = await Assert.That(fields.Length).IsGreaterThanOrEqualTo(6);
        _ = await Assert.That(inStep).IsEmpty();
    }

    [Test]
    public async Task ARecipeCardsFirstWash_WearsItsTilesColour_AndArrivesFirst()
    {
        await RecipeIndex.CurrentAsync(Page);
        await Visit.BeginAsync(Page.Context, Site.BaseUrl, 300);
        await Page.GotoAsync(Library);
        await Washes.SettleAsync(Page);

        var cards = await Page.EvaluateAsync<CardReading[]>(
            """
            () => [...document.querySelectorAll('.kcc-recipe')].filter((card) => card.querySelector('.kcc-tile')).map((card) => {
              const washes = [...card.querySelectorAll('.kcc-sheet > .kcc-washes > .kcc-wash')]
              const at = (wash) => parseFloat(getComputedStyle(wash).getPropertyValue('--wash-at'))
              const colourOf = (parent, value) => {
                const probe = document.createElement('span')
                probe.style.color = value
                parent.append(probe)
                const colour = getComputedStyle(probe).color
                probe.remove()
                return colour
              }
              return {
                tile: colourOf(card.querySelector('.kcc-tile'), 'var(--c)'),
                first: washes[0] ? colourOf(washes[0], 'var(--wash-ink)') : '',
                arrivesFirst: washes.length > 0 && washes.every((wash) => at(washes[0]) <= at(wash)),
              }
            })
            """);

        _ = await Assert.That(cards).IsNotEmpty();
        _ = await Assert.That(cards.Where(card => card.First != card.Tile || !card.ArrivesFirst).Select(card => $"{card.Tile} {card.First} {card.ArrivesFirst}")).IsEmpty();
    }

    private static string Variety(Washes.Wash wash) =>
        string.Join(';', VarietyProperty().Matches(wash.Style).Select(match => match.Value));

    [GeneratedRegex(@"--wash-(x|y|d|turn|ink):[^;]+")]
    private static partial Regex VarietyProperty();

    private sealed class InkReading
    {
        public string[] Brand { get; set; } = [];

        public string[] Inks { get; set; } = [];
    }

    private sealed class CardReading
    {
        public string Tile { get; set; } = string.Empty;

        public string First { get; set; } = string.Empty;

        public bool ArrivesFirst { get; set; }
    }
}
