using System.Text.RegularExpressions;
using KCC.IntegrationTests.Config;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Services;

namespace KCC.IntegrationTests.Features.Baseline;

public class SchemaTests
{
    private static readonly string[] BaselineContentTypeAliases =
    [
        "homePage",
        "recipeListingPage",
        "createRecipePage",
        "addVariantPage",
        "accountPage",
        "loginPage",
        "accountSettingsPage",
        "registrationCompletePage",
        "siteSettings",
        "contentFolder",
        "recipeCategory",
        "recipeTag",
        "statusCodePage",
        "metadata",
        "navLink",
        "navGroup",
    ];

    private static readonly string[] BaselineDataTypeNames =
        ["KCC Show When", "KCC Single Link", "KCC Links", "KCC Nav Items"];

    private static readonly Regex BackofficeUuidPattern =
        new("^[0-9a-f]{8}-[0-9a-f]{4}-[1-8][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$");

    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    [Test]
    [Arguments("homePage")]
    [Arguments("recipeListingPage")]
    [Arguments("createRecipePage")]
    [Arguments("addVariantPage")]
    [Arguments("accountPage")]
    [Arguments("loginPage")]
    [Arguments("accountSettingsPage")]
    [Arguments("registrationCompletePage")]
    [Arguments("siteSettings")]
    [Arguments("contentFolder")]
    [Arguments("recipeCategory")]
    [Arguments("recipeTag")]
    [Arguments("statusCodePage")]
    [Arguments("metadata")]
    [Arguments("navLink")]
    [Arguments("navGroup")]
    public async Task DocumentType_IsImportedOnFirstBoot(string alias)
    {
        var contentTypes = Site.Services.GetRequiredService<IContentTypeService>();

        _ = await Assert.That(contentTypes.Get(alias)).IsNotNull();
    }

    [Test]
    public async Task HomePage_ComposesMetadata()
    {
        var home = Site.Services.GetRequiredService<IContentTypeService>().Get("homePage")!;

        _ = await Assert.That(home.ContentTypeComposition.Any(type => type.Alias == "metadata")).IsTrue();
    }

    [Test]
    public async Task NavElements_AreElementTypes()
    {
        var contentTypes = Site.Services.GetRequiredService<IContentTypeService>();

        _ = await Assert.That(contentTypes.Get("navLink")!.IsElement).IsTrue();
        _ = await Assert.That(contentTypes.Get("navGroup")!.IsElement).IsTrue();
    }

    [Test]
    public async Task BaselineKeys_AreUuidsTheBackofficeAccepts()
    {
        var contentTypes = Site.Services.GetRequiredService<IContentTypeService>();
        var dataTypes = Site.Services.GetRequiredService<IDataTypeService>();

        var keys = BaselineContentTypeAliases.Select(alias => (Name: alias, contentTypes.Get(alias)!.Key)).ToList();
        foreach (var name in BaselineDataTypeNames)
        {
            keys.Add((name, (await dataTypes.GetAsync(name))!.Key));
        }

        var rejected = keys
            .Where(entry => !BackofficeUuidPattern.IsMatch(entry.Key.ToString()))
            .Select(entry => $"{entry.Name} {entry.Key}")
            .ToList();

        _ = await Assert.That(rejected).IsEmpty();
    }
}
