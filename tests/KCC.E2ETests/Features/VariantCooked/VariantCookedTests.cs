using KCC.E2ETests.Config;
using Microsoft.Playwright;

namespace KCC.E2ETests.Features.VariantCooked;

[NotInParallel(MemberSession.Serial)]
public class VariantCookedTests : BasePageTests
{
    [Test]
    public async Task LoggedInMember_TogglingICookedThis_ChangesTheCount()
    {
        await MemberSession.SignInAsync(Page);
        _ = await Page.GotoAsync(MemberTestVariant.Path);

        var toggle = Page.Locator("[data-testid='cooked-toggle']");
        await Expect(toggle).ToBeVisibleAsync();

        var before = await ReadCount(toggle);
        await toggle.ClickAsync();
        await Expect(toggle).Not.ToContainTextAsync($"({before})");
        _ = await Assert.That(await ReadCount(toggle)).IsEqualTo(before + 1);

        // Untoggle so the run is repeatable.
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
