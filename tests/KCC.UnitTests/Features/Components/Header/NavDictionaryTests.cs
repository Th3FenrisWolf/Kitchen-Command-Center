using System.Xml.Linq;

namespace KCC.UnitTests.Features.Components.Header;

// A key with no item renders as its own name, so every label the pad uses is pinned here, words and all.
public class NavDictionaryTests
{
    private static readonly Dictionary<string, string> Words = new()
    {
        ["Nav.Recipes"] = "Recipes",
        ["Nav.MyKitchen"] = "My kitchen",
        ["Nav.Menu"] = "Menu",
        ["Nav.SignIn"] = "Sign in",
        ["Nav.SignOut"] = "Sign out",
        ["Nav.NewRecipe"] = "New recipe",
        ["Nav.AskForAnAccount"] = "Ask for an account",
        ["Nav.Meals"] = "Meals",
        ["Nav.Diets"] = "Diets",
        ["Nav.QuickPicks"] = "Quick picks",
        ["Nav.UnderThirtyMinutes"] = "Under 30 minutes",
        ["Nav.TopRated"] = "Top rated",
        ["Nav.MostVariants"] = "Most variants",
        ["Nav.Newest"] = "Newest",
        ["Nav.SurpriseMe"] = "Surprise me",
        ["Nav.AllRecipes"] = "All {0} recipes",
        ["Nav.SearchPlaceholder"] = "Search {0} recipes",
        ["Nav.RecentlyViewed"] = "Recently viewed",
        ["Nav.Try"] = "Try",
        ["Nav.NothingMatches"] = "Nothing matches “{0}” yet.",
        ["Nav.AllResults"] = "All {0} results",
        ["Nav.SearchUnavailable"] = "Search is unavailable",
        ["Nav.SignedInAs"] = "Signed in as",
        ["Nav.MemberSince"] = "Member since",
        ["Nav.YourRecipesAndVariants"] = "Your recipes and variants",
        ["Nav.WaitingForReview"] = "Waiting for review",
        ["Nav.Settings"] = "Settings",
        ["Nav.Variants"] = "Variants",
        ["Nav.KitchenOf"] = "{0}’s kitchen",
    };

    [Test]
    public async Task NavGroup_HoldsEveryLabelThePadUses_InItsWords()
    {
        var labels = NavItems().Where(item => item.Alias != "Nav").Select(item => $"{item.Alias} = {item.Text}").Order(StringComparer.Ordinal);
        var expected = Words.Select(word => $"{word.Key} = {word.Value}").Order(StringComparer.Ordinal);

        _ = await Assert.That(string.Join("\n", labels)).IsEqualTo(string.Join("\n", expected));
    }

    [Test]
    public async Task NavGroup_IsTopLevel_WithEveryLabelBeneathIt()
    {
        var items = NavItems().ToList();

        _ = await Assert.That(items.Single(item => item.Alias == "Nav").Level).IsEqualTo("0");
        _ = await Assert.That(items.Where(item => item.Alias != "Nav").All(item => item.Level == "1" && item.Parent == "Nav")).IsTrue();
    }

    private static IEnumerable<(string Alias, string Level, string Parent, string Text)> NavItems() =>
        Directory.GetFiles(Path.Combine(RepoPaths.WebProject, "uSync", "v17", "Dictionary"), "nav*.config")
            .Select(path => XDocument.Load(path).Root)
            .Select(root => (
                root.Attribute("Alias").Value,
                root.Attribute("Level").Value,
                root.Element("Info").Element("Parent")?.Value,
                root.Element("Translations").Elements("Translation").SingleOrDefault(translation => translation.Attribute("Language").Value == "en-US")?.Value));
}
