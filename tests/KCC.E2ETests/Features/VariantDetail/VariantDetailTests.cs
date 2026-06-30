using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace KCC.E2ETests.Features.VariantDetail;

public class VariantDetailTests : BasePageTests
{
    // Navigate to the first available Recipe Variant detail page. Variant URLs are
    // CMS-generated, so we reach one through the site rather than a hard-coded slug.
    private async Task<IResponse?> GotoFirstVariantAsync()
    {
        var home = await Page.GotoAsync("/");
        _ = await Assert.That(home).IsNotNull();
        _ = await Assert.That(home!.Ok).IsTrue();

        // A variant detail page renders the "Per Serving" nutrition card; the recipe
        // detail page does not. Follow links until we land on one, starting from the
        // first recipe link on the home page.
        var recipeLink = Page.GetByRole(AriaRole.Link).Filter(new() { HasTextRegex = new("/recipes/", RegexOptions.IgnoreCase) }).First;
        await recipeLink.ClickAsync();

        // From the recipe page, open its first variant.
        var variantLink = Page.GetByRole(AriaRole.Link).Filter(new() { HasTextRegex = new("/variants/", RegexOptions.IgnoreCase) }).First;
        return await variantLink.ClickAsync().ContinueWith(_ => Page.MainFrame.ParentFrame is null ? null : (IResponse?)null);
    }

    [Test]
    public async Task NutritionCard_RendersHeading_OnVariantDetail()
    {
        await GotoFirstVariantAsync();

        // The "Per Serving" nutrition card is always present on a variant page.
        var card = Page.Locator("div", new() { HasText = "Nutrition" }).First;
        await Expect(card).ToBeVisibleAsync();
        await Expect(Page.GetByText("Per Serving", new() { Exact = false })).ToBeVisibleAsync();
    }

    [Test]
    public async Task NutritionCard_FullValues_ShowsAllRows()
    {
        // PREREQ: navigate to a variant whose content has all seven macros set
        // (Calories/Protein/Carbs/Fat/Fiber/Sugar/Sodium). Seed in Kentico admin if absent.
        await GotoFirstVariantAsync();

        var rows = Page.Locator("dl dt");
        // Full nutrition renders one <dt> per provided macro.
        await Expect(rows).ToHaveCountAsync(7);
    }

    [Test]
    public async Task NutritionCard_PartialValues_OmitsBlankRows()
    {
        // PREREQ: a variant with only some macros set. Blank fields are omitted, so the
        // visible row count is strictly between 1 and 7 and the empty state is absent.
        await GotoFirstVariantAsync();

        var rows = Page.Locator("dl dt");
        var count = await rows.CountAsync();
        _ = await Assert.That(count).IsGreaterThan(0);
        _ = await Assert.That(count).IsLessThan(7);
        await Expect(Page.GetByText("not provided", new() { Exact = false })).ToHaveCountAsync(0);
    }

    [Test]
    public async Task NutritionCard_NoValues_ShowsEmptyState()
    {
        // PREREQ: a variant with no nutrition values at all.
        await GotoFirstVariantAsync();

        // The "not provided" empty state shows and no macro rows render.
        await Expect(Page.GetByText("not provided", new() { Exact = false })).ToBeVisibleAsync();
        await Expect(Page.Locator("dl dt")).ToHaveCountAsync(0);
    }

    [Test]
    public async Task DifficultyTile_Set_ShowsLabelAndDot()
    {
        // PREREQ: a variant with Difficulty set (easy/medium/hard).
        await GotoFirstVariantAsync();

        // The difficulty tile shows the "Difficulty" label and a colored status dot,
        // and is not the disabled "coming soon" badge. StatTiles.vue renders the dot as
        // <i class="fa-solid fa-circle fa-xs text-{color}"> (a Font Awesome icon), so the
        // dot is matched by i.fa-circle — NOT span.rounded-full.
        var difficultyTile = Page.Locator("section >> text=Difficulty").First;
        await Expect(difficultyTile).ToBeVisibleAsync();
        await Expect(Page.Locator("i.fa-circle")).Not.ToHaveCountAsync(0);
    }

    [Test]
    public async Task DifficultyTile_Unset_IsOmitted()
    {
        // PREREQ: a variant with Difficulty unset → the tile is omitted from the stat row.
        await GotoFirstVariantAsync();

        await Expect(Page.GetByText("Difficulty", new() { Exact = true })).ToHaveCountAsync(0);
    }
}
