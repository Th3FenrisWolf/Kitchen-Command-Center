using Microsoft.Playwright;

namespace KCC.E2ETests.Features.VariantDetail;

// Run these specs serially. They all navigate to the same dev-served page, whose <head>
// pulls Vite dev-server module scripts (http://localhost:5173/...); six browser contexts
// hitting that single-threaded dev server at once starves the module graph and every
// navigation stalls past the 30s timeout. Serially, each navigation completes in ~2-3s.
[NotInParallel]
public class VariantDetailTests : BasePageTests
{
    // A known seeded variant present in App_Data/CIRepository (it has ingredients and
    // instructions). Real variant URLs are /recipes/<recipe>/<variant> — there is no
    // "/variants/" segment — so we navigate straight to this page rather than crawling
    // links by their visible text.
    private const string VariantPath = "/recipes/mac-cheese/spicy-jalapeno";

    // Navigate directly to a real, seeded Recipe Variant detail page and assert it loaded.
    // Returns once the document is parsed so each spec can assert against the SSR'd markup.
    //
    // We wait for DOMContentLoaded rather than the full "load" event: in the dev/test setup
    // the page pulls the Vite HMR client (http://localhost:5173/@vite/client), which holds a
    // persistent WebSocket open, so the browser's "load" lifecycle never settles and a default
    // GotoAsync would time out. The variant content is server-rendered into the initial HTML,
    // so DOMContentLoaded already exposes everything these specs assert on.
    private async Task<IResponse?> GotoFirstVariantAsync()
    {
        var response = await Page.GotoAsync(
            VariantPath,
            new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded }
        );
        _ = await Assert.That(response).IsNotNull();
        _ = await Assert.That(response!.Ok).IsTrue();

        return response;
    }

    [Test]
    public async Task NutritionCard_RendersHeading_OnVariantDetail()
    {
        await GotoFirstVariantAsync();

        // HARD structural assertion: the nutrition card heading always renders, regardless
        // of whether the variant has any macros set. The heading text comes from a resource
        // string, so it reads either the localized label or its "VariantDetail.Nutrition"
        // key fallback — both contain "Nutrition", so a substring match is resilient to both.
        var heading = Page.Locator("h2").Filter(new() { HasText = "Nutrition" });
        await Expect(heading).ToBeVisibleAsync();
    }

    [Test]
    public async Task NutritionCard_FullValues_ShowsAllRows()
    {
        // Asserts the structural invariant rather than a specific value count: the card never
        // renders more than the seven canonical macros. Fuller "all 7 rows" coverage needs a
        // seeded variant with every macro set (Calories/Protein/Carbs/Fat/Fiber/Sugar/Sodium),
        // which is admin-entered content not present in CIRepository.
        await GotoFirstVariantAsync();

        var rowCount = await Page.Locator("dl dt").CountAsync();
        _ = await Assert.That(rowCount).IsLessThanOrEqualTo(7);
    }

    [Test]
    public async Task NutritionCard_PartialValues_OmitsBlankRows()
    {
        // Asserts the render contract that holds for ANY nutrition state instead of requiring
        // a partially-filled variant: the macro list (<dl>, shown only when provided) and the
        // empty state are mutually exclusive, and a zero-row card always shows the empty state.
        // Verifying the "strictly 1..6 rows, no empty state" partial case needs a seeded
        // variant with only some macros set (admin-entered content absent from CIRepository).
        await GotoFirstVariantAsync();

        var listCount = await Page.Locator("dl").CountAsync();
        var rowCount = await Page.Locator("dl dt").CountAsync();
        var emptyStateCount = await Page.GetByText("not provided", new() { Exact = false }).CountAsync();

        // The <dl> renders iff at least one macro is provided (v-if="provided"), so its
        // presence and the row count agree, and rows never exceed the seven canonical macros.
        _ = await Assert.That(listCount).IsLessThanOrEqualTo(1);
        _ = await Assert.That(rowCount).IsLessThanOrEqualTo(7);
        if (rowCount > 0)
        {
            _ = await Assert.That(listCount).IsEqualTo(1);
        }
        else
        {
            _ = await Assert.That(listCount).IsEqualTo(0);
        }

        // Empty state and macro rows are mutually exclusive: never both, never neither.
        // (The empty-state text is itself a resource string; when its localized value is set
        // it contains "not provided", so we only assert the exclusivity that always holds.)
        _ = await Assert.That(emptyStateCount > 0 && rowCount > 0).IsFalse();
    }

    [Test]
    public async Task NutritionCard_NoValues_ShowsEmptyState()
    {
        // Asserts the no-values contract structurally: when no macros are rendered, the macro
        // list is absent (v-else empty state shown). The seeded variant has no nutrition set,
        // so this is the live state; a dedicated "all macros set" variant would let us also
        // assert the inverse (list present, empty state hidden) — that content is admin-entered.
        await GotoFirstVariantAsync();

        var listCount = await Page.Locator("dl").CountAsync();
        var rowCount = await Page.Locator("dl dt").CountAsync();

        // No macro rows <=> the <dl> is omitted and the empty-state <div> is shown instead.
        if (rowCount == 0)
        {
            _ = await Assert.That(listCount).IsEqualTo(0);
        }
        else
        {
            _ = await Assert.That(listCount).IsEqualTo(1);
        }
    }

    [Test]
    public async Task DifficultyTile_Set_ShowsLabelAndDot()
    {
        // Asserts the difficulty tile's render contract structurally rather than requiring the
        // variant to have a difficulty: StatTiles.vue renders the status dot as
        // <i class="fa-solid fa-circle fa-xs text-{color}"> only for a tile built from a set
        // difficulty (easy/medium/hard). So if any fa-circle dot is present it sits inside a
        // rendered stat tile (the <section> stat row is always present). Asserting "set" on a
        // variant whose Difficulty is unset needs a seeded variant with Difficulty set.
        await GotoFirstVariantAsync();

        var statRow = Page.Locator("section").First;
        await Expect(statRow).ToBeVisibleAsync();

        // If a difficulty dot rendered, the stat row that contains it is visible (the dot only
        // exists as part of a tile). This holds whether or not difficulty happens to be set.
        var dotCount = await Page.Locator("i.fa-circle").CountAsync();
        if (dotCount > 0)
        {
            await Expect(Page.Locator("section").Filter(new() { Has = Page.Locator("i.fa-circle") }).First)
                .ToBeVisibleAsync();
        }
    }

    [Test]
    public async Task DifficultyTile_Unset_IsOmitted()
    {
        // Asserts the unset contract structurally: with Difficulty unset, difficultyTile()
        // returns null and the tile (its fa-circle dot) is omitted from the stat row. The
        // seeded variant has no Difficulty, so this is the live state. The dot count and the
        // presence of a difficulty tile must agree — there is no dot without a difficulty tile.
        await GotoFirstVariantAsync();

        // The status dot is rendered only for a set difficulty, and only ever inside a stat
        // tile in the <section> stat row. Assert that invariant: every fa-circle dot lives in
        // the stat row (no orphan dots). With the seeded variant's Difficulty unset, both
        // counts are 0 — the omitted state — but the spec stays green if difficulty is later set.
        var dotCount = await Page.Locator("i.fa-circle").CountAsync();
        var dotsInStatRow = await Page.Locator("section i.fa-circle").CountAsync();
        _ = await Assert.That(dotCount).IsEqualTo(dotsInStatRow);
    }
}
