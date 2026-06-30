using KCC.E2ETests.Config;
using Microsoft.Playwright;

namespace KCC.E2ETests.Features.VariantCooked;

public class VariantCookedTests : BasePageTests
{
    [Test]
    public async Task LoggedInMember_TogglingICookedThis_ChangesTheCount()
    {
        _ = await Assert.That(MemberSession.HasCredentials)
            .IsTrue()
            .Because("Set KCC_E2E_MEMBER_USERNAME/PASSWORD to a seeded confirmed member to run this flow.");

        await MemberSession.SignInAsync(Page);
        _ = await RecipeNavigation.GoToFirstRecipeAsync(Page);
        _ = await RecipeNavigation.GoToFirstVariantAsync(Page);

        var toggle = Page.Locator("[data-testid='cooked-toggle']");
        await Expect(toggle).ToBeVisibleAsync();

        var before = await ReadCount(toggle);
        await toggle.ClickAsync();
        await Expect(toggle).Not.ToContainTextAsync($"({before})"); // count changed after the round-trip

        var after = await ReadCount(toggle);
        _ = await Assert.That(after).IsNotEqualTo(before);

        // Toggle back so the test is idempotent across runs.
        await toggle.ClickAsync();
        await Expect(toggle).ToContainTextAsync($"({before})");
    }

    private static async Task<int> ReadCount(ILocator toggle)
    {
        var text = await toggle.InnerTextAsync();
        var digits = new string(text.Where(char.IsDigit).ToArray());
        return digits.Length == 0 ? 0 : int.Parse(digits);
    }
}
