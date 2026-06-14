using KCC.Admin;

namespace KCC.UnitTests.Admin;

public class RecipeIconsTests
{
    [Test]
    public async Task All_IsNotEmpty() => _ = await Assert.That(RecipeIcons.All.Any()).IsTrue();

    [Test]
    public async Task Fallback_IsDeterministic_ForSameSeed()
    {
        _ = await Assert.That(RecipeIcons.Fallback("Spaghetti Bolognese")).IsEqualTo(RecipeIcons.Fallback("Spaghetti Bolognese"));
    }

    [Test]
    public async Task Fallback_ReturnsAMemberOfAll() => _ = await Assert.That(RecipeIcons.All).Contains(RecipeIcons.Fallback("anything"));

    [Test]
    public async Task IsKnown_TrueForListedIcon_FalseForUnlisted()
    {
        _ = await Assert.That(RecipeIcons.IsKnown(RecipeIcons.All[0])).IsTrue();
        _ = await Assert.That(RecipeIcons.IsKnown("definitely-not-an-icon")).IsFalse();
    }

    [Test]
    public async Task All_AreFullFontAwesomeClassStrings()
    {
        _ = await Assert.That(RecipeIcons.All.All(icon => System.Text.RegularExpressions.Regex.IsMatch(icon, @"^fa-[a-z]+ fa-[a-z0-9-]+$"))).IsTrue();
    }
}
