using KCC.Admin;
using Xunit;

namespace KCC.Web.Tests.Admin;

public class RecipeIconsTests
{
    [Fact]
    public void All_IsNotEmpty() => Assert.NotEmpty(RecipeIcons.All);

    [Fact]
    public void Fallback_IsDeterministic_ForSameSeed()
    {
        Assert.Equal(RecipeIcons.Fallback("Spaghetti Bolognese"), RecipeIcons.Fallback("Spaghetti Bolognese"));
    }

    [Fact]
    public void Fallback_ReturnsAMemberOfAll() => Assert.Contains(RecipeIcons.Fallback("anything"), RecipeIcons.All);

    [Fact]
    public void IsKnown_TrueForListedIcon_FalseForUnlisted()
    {
        Assert.True(RecipeIcons.IsKnown(RecipeIcons.All[0]));
        Assert.False(RecipeIcons.IsKnown("definitely-not-an-icon"));
    }

    [Fact]
    public void All_AreFullFontAwesomeClassStrings()
    {
        Assert.All(RecipeIcons.All, icon => Assert.Matches(@"^fa-[a-z]+ fa-[a-z0-9-]+$", icon));
    }
}
