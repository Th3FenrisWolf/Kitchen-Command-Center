using System.Xml.Linq;
using KCC.IntegrationTests.Config;
using KCC.Web.Features.Dictionary;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Services;

namespace KCC.IntegrationTests.Features.Baseline;

public class DictionaryTests
{
    private static readonly string[] RetiredAddVariantKeys =
    [
        "AddIngredient", "AddStep", "Back", "CookTime", "DescribeThisStep", "Description", "Eyeball", "IngredientName",
        "IngredientNamePlaceholder", "Ingredients", "Instructions", "Min", "Next", "PrepTime", "Quantity", "QuantityPlaceholder",
        "ReviewAndSubmit", "Serves", "Servings", "Step", "Steps", "SubmitForReview", "Submitting", "ToTaste", "UnexpectedError",
        "Unit", "UnitPlaceholder", "VariantInfo", "VariantName", "VariantSubmitted", "VariantSubmittedMessage",
        "DescriptionPlaceholder", "Remove", "FailedToAddVariant",
    ];

    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    [Test]
    public async Task ExportedString_IsImportedOnFirstBoot()
    {
        using var scope = Site.Services.CreateScope();
        var strings = scope.ServiceProvider.GetRequiredService<IResourceStringProvider>();

        _ = await Assert.That(strings.GetOrDefault("Login.SignIn")).IsNotEqualTo("Login.SignIn");
        _ = await Assert.That(strings.GetOrDefault("Shared.LogoAlt")).IsEqualTo("Kitchen Command Center");
    }

    [Test]
    public async Task UnknownKey_FallsBackToTheKey()
    {
        using var scope = Site.Services.CreateScope();
        var strings = scope.ServiceProvider.GetRequiredService<IResourceStringProvider>();

        _ = await Assert.That(strings.GetOrDefault("Nowhere.ToBeFound")).IsEqualTo("Nowhere.ToBeFound");
    }

    [Test]
    public async Task NavGroup_IsImportedOnFirstBoot()
    {
        using var scope = Site.Services.CreateScope();
        var nav = scope.ServiceProvider.GetRequiredService<IResourceStringProvider>().GetGroup("Nav");

        _ = await Assert.That(nav.Count).IsEqualTo(29);
        _ = await Assert.That(nav["Nav.MyKitchen"]).IsEqualTo("My kitchen");
        _ = await Assert.That(nav["Nav.NothingMatches"]).IsEqualTo("Nothing matches “{0}” yet.");
        _ = await Assert.That(nav["Nav.KitchenOf"]).IsEqualTo("{0}’s kitchen");
    }

    [Test]
    public async Task RecipeWizardGroup_ImportsEveryHandWrittenItem_WithItsValue()
    {
        var written = HandWrittenItems("recipewizard.*.config");
        using var scope = Site.Services.CreateScope();
        var group = scope.ServiceProvider.GetRequiredService<IResourceStringProvider>().GetGroup("RecipeWizard");
        var unmatched = written.Where(item => group.GetValueOrDefault(item.Key) != item.Value).Select(item => item.Key);

        _ = await Assert.That(group).IsNotEmpty();
        _ = await Assert.That(unmatched).IsEmpty();
    }

    [Test]
    public async Task AddVariantGroup_KeepsOnlyItsHeroAndClosingLinkItems()
    {
        var dictionary = Site.Services.GetRequiredService<IDictionaryItemService>();

        var retired = await dictionary.GetManyAsync([.. RetiredAddVariantKeys.Select(key => $"AddVariant.{key}")]);
        var kept = await dictionary.GetManyAsync("AddVariant.AddVariantFor", "AddVariant.BackTo", "AddVariant.Cancel");

        _ = await Assert.That(retired.Select(item => item.ItemKey)).IsEmpty();
        _ = await Assert.That(kept.Select(item => item.ItemKey))
            .IsEquivalentTo(["AddVariant.AddVariantFor", "AddVariant.BackTo", "AddVariant.Cancel"]);
    }

    private static Dictionary<string, string> HandWrittenItems(string pattern) =>
        Directory.GetFiles(Path.Combine(RepoPaths.WebProject, "uSync", "v17", "Dictionary"), pattern)
            .Select(path => XDocument.Load(path).Root!)
            .ToDictionary(item => item.Attribute("Alias")!.Value, EnglishValue, StringComparer.Ordinal);

    private static string EnglishValue(XElement item) =>
        item.Element("Translations")!.Elements("Translation").Single(translation => translation.Attribute("Language")!.Value == "en-US").Value;
}
