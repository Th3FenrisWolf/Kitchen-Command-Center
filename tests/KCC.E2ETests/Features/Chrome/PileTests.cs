using System.Text.Json;
using System.Text.RegularExpressions;
using KCC.E2ETests.Config;
using Microsoft.Playwright;

namespace KCC.E2ETests.Features.Chrome;

public class PileTests : BasePageTests
{
    private const string AllRecipes = "/recipes/";

    // No beverage is rated, so nothing takes the spotlight.
    private const string NoSpotlight = "/recipes/?category=Beverage";

    // Only Weeknight Bowls lists tahini, and it is rated, so it shows as the spotlight alone.
    private const string SpotlightAlone = "/recipes/?query=tahini";

    private const string NoMatches = "/recipes/?query=zzznotarealrecipe";

    private const string WithVariants = "/recipes/spicy-ramen-flight/";

    private const string NoVariants = "/recipes/bare-cupboard-snack-board/";

    private const string ReviewedVariant = "/recipes/spicy-ramen-flight/chili-oil-shoyu";

    private const string UnreviewedVariant = "/recipes/spicy-ramen-flight/miso-vegan";

    private const int SeededReviews = 3;

    private const int Siblings = 3;

    private const int ComingSoonSheets = 2;

    private const int HomeSections = 6;

    private const int StubbedFirstPage = 10;

    // Odd, so the second page moves the review section's last sheet to the other turn and the siblings re-flip.
    private const int StubbedTotal = 11;

    private const string Reviews = "main [data-testid='reviews-list'] > li";

    private const string FeaturedVariant = "main section .kcc-tear-hero";

    private const string RecipeGroups = "main section ul.grid > li.kcc-slip";

    private const string Spotlight = "main [data-testid='recipe-spotlight']";

    private const string SearchCard = "main [data-testid='recipe-card']";

    private const string FiltersToggle = "button[aria-controls='recipe-filters']";

    private const int FiltersBeside = 1024;

    private const string PadPage = ".pad-page";

    private static readonly double PadPageTurn = 1.005 * Math.Sin((0.5 - 0.12) * Math.PI / 180);

    private static readonly Regex Open = new(@"\bis-open\b");

    private static readonly Regex Tucked = new(@"\bis-tucked\b");

    [Test]
    [Arguments(375, AllRecipes, "grid", false)]
    [Arguments(375, AllRecipes, "grid", true)]
    [Arguments(375, AllRecipes, "list", false)]
    [Arguments(375, AllRecipes, "list", true)]
    [Arguments(375, NoSpotlight, "grid", false)]
    [Arguments(375, NoSpotlight, "grid", true)]
    [Arguments(375, NoSpotlight, "list", false)]
    [Arguments(375, NoSpotlight, "list", true)]
    [Arguments(375, SpotlightAlone, "grid", false)]
    [Arguments(375, SpotlightAlone, "grid", true)]
    [Arguments(375, NoMatches, "grid", false)]
    [Arguments(375, NoMatches, "grid", true)]
    [Arguments(900, AllRecipes, "grid", false)]
    [Arguments(900, AllRecipes, "grid", true)]
    [Arguments(900, AllRecipes, "list", false)]
    [Arguments(900, AllRecipes, "list", true)]
    [Arguments(900, NoSpotlight, "grid", false)]
    [Arguments(900, NoSpotlight, "grid", true)]
    [Arguments(900, NoSpotlight, "list", false)]
    [Arguments(900, NoSpotlight, "list", true)]
    [Arguments(1280, AllRecipes, "grid", false)]
    [Arguments(1280, AllRecipes, "list", false)]
    [Arguments(1280, NoSpotlight, "grid", false)]
    [Arguments(1280, NoSpotlight, "list", false)]
    [Arguments(1280, SpotlightAlone, "grid", false)]
    [Arguments(1280, NoMatches, "grid", false)]
    public async Task RecipeSearch_Alternates(int width, string path, string view, bool filtersOpen)
    {
        await OpenAsync(width, path);
        await ExpectSearchStateAsync(path);

        if (view == "list")
        {
            await ShowListAsync();
        }

        if (width < FiltersBeside)
        {
            await Expect(Page.Locator(FiltersToggle)).ToBeVisibleAsync();
            await Expect(Page.Locator("#recipe-filters")).ToBeHiddenAsync();
        }

        if (filtersOpen)
        {
            await Page.Locator(FiltersToggle).ClickAsync();
            await Expect(Page.Locator("#recipe-filters")).ToBeVisibleAsync();
        }

        await ExpectAlternatingPilesAsync();
    }

    [Test]
    [Arguments(375, "grid")]
    [Arguments(375, "list")]
    [Arguments(900, "grid")]
    [Arguments(900, "list")]
    [Arguments(1280, "grid")]
    [Arguments(1280, "list")]
    public async Task RecipeDetail_WithVariants_Alternates(int width, string view)
    {
        await OpenAsync(width, WithVariants);
        await Expect(Page.Locator(FeaturedVariant)).ToBeVisibleAsync();

        if (view == "list")
        {
            await ShowListAsync();
        }

        await ExpectAlternatingPilesAsync();
    }

    [Test]
    [Arguments(375)]
    [Arguments(1280)]
    public async Task RecipeDetail_SearchThatMatchesNone_LeavesTheFeaturedCardOverTheEmptyState(int width)
    {
        await OpenAsync(width, WithVariants);

        await Page.Locator("main input[type='search']").FillAsync("zzznotavariant");
        await Expect(Page.Locator("main .grid > .kcc-recipe")).ToHaveCountAsync(0);
        await Expect(Page.Locator(FeaturedVariant)).ToBeVisibleAsync();

        await ExpectAlternatingPilesAsync();
    }

    [Test]
    [Arguments(375)]
    [Arguments(900)]
    [Arguments(1280)]
    public async Task RecipeDetail_WithNoVariants_Alternates(int width)
    {
        await OpenAsync(width, NoVariants);
        await Expect(Page.Locator(FeaturedVariant)).ToHaveCountAsync(0);

        await ExpectAlternatingPilesAsync();
    }

    [Test]
    [NotInParallel(MemberSession.Serial)]
    [Arguments(375, "recipe", 0)]
    [Arguments(375, "recipe", 1)]
    [Arguments(375, "recipe", 2)]
    [Arguments(375, "recipe", 3)]
    [Arguments(375, "variant", 0)]
    [Arguments(375, "variant", 1)]
    [Arguments(375, "variant", 2)]
    [Arguments(375, "variant", 3)]
    [Arguments(1280, "recipe", 0)]
    [Arguments(1280, "recipe", 1)]
    [Arguments(1280, "recipe", 2)]
    [Arguments(1280, "recipe", 3)]
    [Arguments(1280, "variant", 0)]
    [Arguments(1280, "variant", 1)]
    [Arguments(1280, "variant", 2)]
    [Arguments(1280, "variant", 3)]
    public async Task RecipeWizard_Alternates(int width, string flow, int step)
    {
        await Page.SetViewportSizeAsync(width, 900);
        await MemberSession.SignInAsync(Page);
        await OpenWizardAsync(flow);
        await Expect(Page.GetByTestId("wizard-start-typing")).ToBeVisibleAsync();

        if (step > 0)
        {
            await Page.GetByTestId("wizard-start-typing").ClickAsync();
        }

        for (var counted = 1; counted <= step; counted++)
        {
            await Expect(Page.Locator("main [aria-current='step']")).ToHaveTextAsync($"{counted:00}{counted}");

            if (counted < step)
            {
                await FillWizardStepAsync(flow, counted);
                await Page.GetByRole(AriaRole.Button, new() { Name = "Next", Exact = true }).ClickAsync();
            }
        }

        await ExpectAlternatingPilesAsync();
    }

    [Test]
    [Arguments(375, true)]
    [Arguments(375, false)]
    [Arguments(1280, true)]
    [Arguments(1280, false)]
    public async Task VariantDetail_SignedOut_Alternates(int width, bool reviewed)
    {
        await OpenAsync(width, reviewed ? ReviewedVariant : UnreviewedVariant);
        await Expect(Page.GetByTestId("review-input")).ToHaveCountAsync(0);
        await ExpectVariantStateAsync(reviewed);

        await ExpectAlternatingPilesAsync();
    }

    [Test]
    [NotInParallel(MemberSession.Serial)]
    [Arguments(375, true)]
    [Arguments(375, false)]
    [Arguments(1280, true)]
    [Arguments(1280, false)]
    public async Task VariantDetail_SignedIn_Alternates(int width, bool reviewed)
    {
        await Page.SetViewportSizeAsync(width, 900);
        await MemberSession.SignInAsync(Page);
        await OpenAsync(width, reviewed ? ReviewedVariant : UnreviewedVariant);
        await Expect(Page.GetByTestId("review-input")).ToBeVisibleAsync();
        await ExpectVariantStateAsync(reviewed);

        await ExpectAlternatingPilesAsync();
    }

    [Test]
    [NotInParallel(MemberSession.Serial)]
    [Arguments(375)]
    [Arguments(1280)]
    public async Task VariantDetail_LoadMore_Alternates(int width)
    {
        await Page.RouteAsync(
            new Regex(@"/api/variant/[^/]+/reviews(\?|$)"),
            route => route.FulfillAsync(new()
            {
                Status = 200,
                ContentType = "application/json",
                Body = StubbedReviewsPage(new Uri(route.Request.Url).Query.Contains("page=1", StringComparison.Ordinal) ? 1 : 0),
            }));
        await Page.SetViewportSizeAsync(width, 900);
        await MemberSession.SignInAsync(Page);
        await OpenAsync(width, ReviewedVariant);
        await Expect(Page.GetByTestId("review-input")).ToBeVisibleAsync();
        await Expect(Page.Locator(Reviews)).ToHaveCountAsync(StubbedFirstPage);

        await ExpectAlternatingPilesAsync();

        await Page.GetByRole(AriaRole.Button, new() { Name = "Load More", Exact = true }).ClickAsync();
        await Expect(Page.Locator(Reviews)).ToHaveCountAsync(StubbedTotal);

        await ExpectAlternatingPilesAsync();
    }

    [Test]
    [NotInParallel(MemberSession.Serial)]
    [Arguments(375)]
    [Arguments(1280)]
    public async Task Account_Alternates(int width)
    {
        await Page.SetViewportSizeAsync(width, 900);
        await MemberSession.SignInAsync(Page);
        await OpenAsync(width, "/account");
        await Expect(Page.Locator("main section h1.kcc-display")).ToBeVisibleAsync();
        await Expect(Page.Locator("main .kcc-slip:has(> .kcc-label .fa-hourglass-half)")).ToHaveCountAsync(ComingSoonSheets);
        var groups = await Page.Locator(RecipeGroups).CountAsync();

        await ExpectAlternatingPilesAsync($"{groups} recipe groups ({(groups % 2 == 0 ? "even" : "odd")})");
    }

    [Test]
    [Arguments(375)]
    [Arguments(1280)]
    public async Task Login_Alternates(int width)
    {
        await OpenAsync(width, "/account/login");
        await Expect(Page.Locator("main input[name='UserName']")).ToBeVisibleAsync();
        await Expect(Page.Locator("main input[name='Password']")).ToBeVisibleAsync();

        await ExpectAlternatingPilesAsync();
    }

    [Test]
    [NotInParallel(MemberSession.Serial)]
    [Arguments(375)]
    [Arguments(1280)]
    public async Task AccountSettings_Alternates(int width)
    {
        await Page.SetViewportSizeAsync(width, 900);
        await MemberSession.SignInAsync(Page);
        await OpenAsync(width, "/account/settings");
        await Expect(Page.GetByTestId("ramp-device")).ToBeVisibleAsync();

        await ExpectAlternatingPilesAsync();
    }

    [Test]
    [Arguments(375)]
    [Arguments(1280)]
    public async Task Home_Alternates(int width)
    {
        await OpenAsync(width, "/");
        await Expect(Page.Locator("section[data-block]")).ToHaveCountAsync(HomeSections);

        await ExpectAlternatingPilesAsync();
    }

    [Test]
    [Arguments(375)]
    [Arguments(1280)]
    public async Task RegistrationComplete_Alternates(int width)
    {
        await OpenAsync(width, "/account/registration-complete/");
        await Expect(Page.Locator("main .kcc-kick")).ToHaveTextAsync("Registered");

        await ExpectAlternatingPilesAsync();
    }

    [Test]
    [Arguments(375)]
    [Arguments(1280)]
    public async Task ErrorPage_Alternates(int width)
    {
        await OpenAsync(width, "/error");
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Level = 1 })).ToContainTextAsync("Something went wrong");

        await ExpectAlternatingPilesAsync();
    }

    [Test]
    [Arguments(375)]
    [Arguments(1280)]
    public async Task Pad_StacksTheCardsBetweenItsTwoSheets(int width)
    {
        await OpenAsync(width, "/");
        await Expect(Page.Locator(".pad-page")).ToHaveCountAsync(1);
        await Expect(Page.Locator(".pad-page")).ToBeVisibleAsync();

        // The page takes no pointer events, so a hit test cannot see it; paint order is read from the computed styles.
        var stack = await Page.Locator(".pad").EvaluateAsync<string[]>(
            """
            pad => ['.pad-top', '.pad-cards', '.pad-page'].map(selector => {
              const sheet = pad.querySelector(selector)
              const style = getComputedStyle(sheet)
              return `${selector} ${sheet.parentElement === pad ? 'in-pad' : 'elsewhere'} ${style.position} ${style.zIndex}`
            })
            """);

        _ = await Assert.That(string.Join(" | ", stack)).IsEqualTo(
            ".pad-top in-pad relative 5 | .pad-cards in-pad absolute 4 | .pad-page in-pad absolute 3");
    }

    // At 375px the desk menus are hidden, and the phone bar's Menu and Find open their cards instead.
    [Test]
    [NotInParallel(MemberSession.Serial)]
    [Arguments(1280, "recipes")]
    [Arguments(1280, "kitchen")]
    [Arguments(375, "menu")]
    [Arguments(375, "find")]
    public async Task Pad_OpensEachMenuBetweenTheTopSheetAndThePage(int width, string menu)
    {
        await Page.SetViewportSizeAsync(width, 900);
        await MemberSession.SignInAsync(Page);
        await OpenAsync(width, "/");
        var card = Page.Locator($"#pad-card-{menu}");

        await Page.Locator($"#pad-menu-{menu}").ClickAsync();
        await Expect(card).ToHaveClassAsync(Open);
        await Expect(card).ToBeVisibleAsync();
        await TransitionsDoneAsync(card);

        var stack = await card.EvaluateAsync<CardStack>(
            """
            card => {
              const pad = card.closest('.pad')
              let layer = card
              while (layer.parentElement !== pad) layer = layer.parentElement
              const top = pad.querySelector(':scope > .pad-top')
              const page = pad.querySelector(':scope > .pad-page')
              const z = (el) => Number(getComputedStyle(el).zIndex)
              const overlaps = (a, b) => {
                const [p, q] = [a.getBoundingClientRect(), b.getBoundingClientRect()]
                return p.left < q.right && q.left < p.right && p.top < q.bottom && q.top < p.bottom
              }
              return {
                layer: layer.className,
                layerPosition: getComputedStyle(layer).position,
                page: z(page),
                cards: z(layer),
                top: z(top),
                overPage: overlaps(card, page),
                underTop: overlaps(card, top),
              }
            }
            """);

        _ = await Assert.That(stack.Layer).IsEqualTo("pad-cards");
        _ = await Assert.That(stack.LayerPosition).IsEqualTo("absolute");
        _ = await Assert.That(stack.Cards).IsGreaterThan(stack.Page);
        _ = await Assert.That(stack.Top).IsGreaterThan(stack.Cards);
        _ = await Assert.That(stack.OverPage).IsTrue();
        _ = await Assert.That(stack.UnderTop).IsTrue();
    }

    [Test]
    [Arguments(375)]
    [Arguments(1280)]
    public async Task Pad_ReturnsFromItsTuckWithItsOnePage(int width)
    {
        await OpenAsync(width, AllRecipes);
        var header = Page.Locator("header.pad-header");
        await Expect(header).Not.ToHaveClassAsync(Tucked);
        await Expect(Page.Locator(PadPage)).ToBeVisibleAsync();

        await WheelAsync(900);
        await Expect(header).ToHaveClassAsync(Tucked);
        await TransitionsDoneAsync(header);
        _ = await Assert.That(await BottomAsync(PadPage)).IsLessThanOrEqualTo(0);
        _ = await Assert.That(await BottomAsync(".pad-top")).IsLessThanOrEqualTo(0);

        await Page.Mouse.WheelAsync(0, -120);
        await Expect(header).Not.ToHaveClassAsync(Tucked);
        await TransitionsDoneAsync(header);

        await Expect(Page.Locator(PadPage)).ToHaveCountAsync(1);
        await Expect(Page.Locator(PadPage)).ToBeVisibleAsync();
        await Expect(Page.Locator(".pad-top")).ToBeVisibleAsync();
        _ = await Assert.That(await BottomAsync(PadPage)).IsGreaterThan(0);
        await Expect(Page.Locator(PadPage)).ToHaveClassAsync(new Regex(@"\bkcc-slip--wide\b"));
        var turn = await Page.Locator(PadPage).EvaluateAsync<double>(
            "page => Number(getComputedStyle(page).transform.match(/^matrix\\(([^)]+)\\)$/)[1].split(',')[1])");
        _ = await Assert.That(turn).IsEqualTo(PadPageTurn).Within(0.0005);
    }

    [Test]
    [Arguments("hover", false)]
    [Arguments("focus", false)]
    [Arguments("hover", true)]
    [Arguments("focus", true)]
    public async Task RecipeCard_LiftsItsTopSheetAndLeavesTheBlank(string reach, bool reducedMotion)
    {
        await Page.EmulateMediaAsync(new() { ReducedMotion = reducedMotion ? ReducedMotion.Reduce : ReducedMotion.NoPreference });
        await OpenAsync(1280, AllRecipes);
        await ExpectSearchStateAsync(AllRecipes);

        var card = Page.Locator(SearchCard).First;
        await Expect(card).ToBeVisibleAsync();
        await card.ScrollIntoViewIfNeededAsync();
        await SettledAsync(card, ":not(:hover, :focus-within)");
        var before = await PileBoxesAsync(card);

        if (reach == "hover")
        {
            _ = await Assert.That(await Page.EvaluateAsync<bool>("matchMedia('(hover: hover)').matches")).IsTrue();
            await card.HoverAsync();
            await SettledAsync(card, ":hover");
        }
        else
        {
            await card.FocusAsync();
            await SettledAsync(card, ":focus-within");
        }

        var expectedTop = reducedMotion ? before.Sheet[1] : before.Sheet[1] - 4;
        var after = await PileBoxesAsync(card);

        _ = await Assert.That(after.Sheet[1]).IsEqualTo(expectedTop).Within(0.5);
        _ = await Assert.That(after.Sheet[0]).IsEqualTo(before.Sheet[0]).Within(0.5);
        for (var edge = 0; edge < before.Blank.Length; edge++)
        {
            _ = await Assert.That(after.Blank[edge]).IsEqualTo(before.Blank[edge]).Within(0.5);
        }
    }

    private static Task TransitionsDoneAsync(ILocator element) =>
        element.EvaluateAsync(
            """
            element => new Promise(resolve => {
              const check = () => (element.getAnimations().length === 0 ? resolve() : requestAnimationFrame(check))
              check()
            })
            """);

    private Task<double> BottomAsync(string selector) =>
        Page.Locator(selector).EvaluateAsync<double>("element => element.getBoundingClientRect().bottom");

    // Playwright's wheel returns while the page is still scrolling smoothly.
    private async Task WheelAsync(int pixels)
    {
        await Page.EvaluateAsync("() => { window.scrollEnded = new Promise(resolve => addEventListener('scrollend', () => resolve(), { once: true })) }");
        await Page.Mouse.WheelAsync(0, pixels);
        await Page.EvaluateAsync("() => window.scrollEnded");
    }

    private static Task SettledAsync(ILocator card, string slipState) =>
        card.EvaluateAsync(
            """
            (link, state) => new Promise(resolve => {
              const settled = () => link.parentElement.matches(state) && link.getAnimations().length === 0
              const check = () => (settled() ? resolve() : requestAnimationFrame(check))
              check()
            })
            """,
            slipState);

    private static async Task<PileBoxes> PileBoxesAsync(ILocator card) =>
        await card.EvaluateAsync<PileBoxes>(
            """
            link => {
              const page = element => {
                const box = element.getBoundingClientRect()
                return [box.left + scrollX, box.top + scrollY, box.width, box.height]
              }
              return { sheet: page(link), blank: page(link.parentElement.querySelector(':scope > .kcc-blank')) }
            }
            """);

    private async Task ExpectVariantStateAsync(bool reviewed)
    {
        await Expect(Page.Locator("main article.kcc-recipe")).ToHaveCountAsync(Siblings);

        if (reviewed)
        {
            await Expect(Page.Locator(Reviews)).ToHaveCountAsync(SeededReviews);
        }
        else
        {
            await Expect(Page.Locator(Reviews)).ToHaveCountAsync(0);
            await Expect(Page.GetByText("No reviews yet", new() { Exact = false })).ToBeVisibleAsync();
        }
    }

    private static string StubbedReviewsPage(int page)
    {
        var first = page * StubbedFirstPage;
        var reviews = Enumerable.Range(first, Math.Min(StubbedFirstPage, StubbedTotal - first)).Select(index => new
        {
            authorName = $"Stub reviewer {index + 1}",
            rating = 4,
            text = $"Stubbed review #{index + 1}",
            created = "2026-10-01T12:00:00Z",
            isMine = false,
        });

        return JsonSerializer.Serialize(new
        {
            average = 4,
            count = StubbedTotal,
            distribution = new[] { 0, 0, 0, StubbedTotal, 0 },
            total = StubbedTotal,
            page,
            pageSize = StubbedFirstPage,
            reviews,
            myReview = (object?)null,
        });
    }

    private async Task ExpectSearchStateAsync(string path)
    {
        switch (path)
        {
            case AllRecipes:
                await Expect(Page.Locator(Spotlight)).ToBeVisibleAsync();
                break;
            case SpotlightAlone:
                await Expect(Page.Locator(Spotlight)).ToHaveAttributeAsync("data-recipe-name", "Weeknight Bowls");
                await Expect(Page.Locator(SearchCard)).ToHaveCountAsync(0);
                break;
            case NoSpotlight:
                await Expect(Page.Locator(Spotlight)).ToHaveCountAsync(0);
                break;
            case NoMatches:
                await Expect(Page.Locator(Spotlight)).ToHaveCountAsync(0);
                await Expect(Page.Locator("[data-testid='recipes-empty']")).ToBeVisibleAsync();
                break;
        }
    }

    private async Task OpenWizardAsync(string flow)
    {
        if (flow == "recipe")
        {
            _ = await Page.GotoAsync("/recipes/create-recipe");
        }
        else
        {
            _ = await Page.GotoAsync(PhotoImport.RecipePath);
            await Page.GetByRole(AriaRole.Link, new() { Name = "Add Variant", Exact = true }).First.ClickAsync();
            await Page.WaitForURLAsync(url => new Uri(url).Query.StartsWith("?recipe=", StringComparison.Ordinal));
        }

        await Hydration.WaitAsync(Page);
    }

    private async Task FillWizardStepAsync(string flow, int counted)
    {
        switch ((flow, counted))
        {
            case ("recipe", 1):
                await Page.GetByTestId("wizard-recipe-name").FillAsync("Pile Walk");
                await Page.GetByTestId("wizard-recipe-description").FillAsync("Typed by the pile walker.");
                break;
            case ("recipe", 2):
            case ("variant", 1):
                await Page.GetByTestId("wizard-variant-name").FillAsync("First Pile");
                break;
            case ("variant", 2):
                await Page.GetByTestId("wizard-ingredient-name").First.FillAsync("Eggs");
                break;
        }
    }

    private async Task OpenAsync(int width, string path)
    {
        await Page.SetViewportSizeAsync(width, 900);
        await Page.GotoAsync(path);
        await Hydration.WaitAsync(Page);
    }

    private async Task ShowListAsync()
    {
        await Page.Locator("[data-testid='view-list']").ClickAsync();
        await Expect(Page.Locator("main .grid > .kcc-recipe")).ToHaveCountAsync(0);
    }

    private async Task ExpectAlternatingPilesAsync(string? state = null)
    {
        var faults = (await Piles.FaultsAsync(Page)).ToList();
        if (faults.Count > 0 && state is not null)
        {
            faults.Insert(0, $"state: {state}");
        }

        _ = await Assert.That(string.Join(Environment.NewLine, faults)).IsEmpty();
    }

    private sealed class CardStack
    {
        public string Layer { get; init; } = string.Empty;

        public string LayerPosition { get; init; } = string.Empty;

        public int Page { get; init; }

        public int Cards { get; init; }

        public int Top { get; init; }

        public bool OverPage { get; init; }

        public bool UnderTop { get; init; }
    }

    private sealed class PileBoxes
    {
        public double[] Sheet { get; init; } = [];

        public double[] Blank { get; init; } = [];
    }
}
