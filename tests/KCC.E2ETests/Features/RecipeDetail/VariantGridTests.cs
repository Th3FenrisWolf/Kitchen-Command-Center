using KCC.E2ETests.Config;

namespace KCC.E2ETests.Features.RecipeDetail;

public class VariantGridTests : BasePageTests
{
    [Test]
    public async Task Variant_cards_and_the_add_variant_sheet_in_a_row_share_one_height()
    {
        // Spicy Ramen Flight has four variants, so the grid holds five slips with the Add Variant sheet.
        await Page.GotoAsync("/recipes/spicy-ramen-flight/");

        var rows = await GridRows.SheetHeightsAsync(Page.Locator("main .grid:has(> .kcc-recipe)"));

        _ = await Assert.That(rows.Sum(row => row.Length)).IsEqualTo(5);
        _ = await Assert.That(rows.Any(row => row.Length > 1)).IsTrue();
        foreach (var row in rows)
        {
            _ = await Assert.That(row.Max() - row.Min()).IsLessThanOrEqualTo(1);
        }
    }
}
