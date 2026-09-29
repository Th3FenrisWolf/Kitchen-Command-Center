using KCC.Web.Features.Pages.Home;

namespace KCC.UnitTests.Features.Pages.Home;

public class HomeCardTests
{
    private static readonly HomeLink Recipes = new("View Recipes", "/recipes/", null);

    [Test]
    public async Task HasDrawer_WithABody()
    {
        _ = await Assert.That(new HomeCard("Beef", "Bold", "Sizzle.", "bg-red", null, 1).HasDrawer).IsTrue();
    }

    [Test]
    public async Task HasDrawer_WithOnlyALink()
    {
        _ = await Assert.That(new HomeCard("Cake", "Rich", null, "bg-red", Recipes, 1).HasDrawer).IsTrue();
    }

    [Test]
    public async Task HasNoDrawer_WithNeither()
    {
        _ = await Assert.That(new HomeCard("Gin", "Botanical", " ", "bg-green", null, 1).HasDrawer).IsFalse();
    }
}
