using KCC.IntegrationTests.Config;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;

namespace KCC.IntegrationTests.Features.Baseline;

public class SchemaTests
{
    private static readonly string[] SchemaKeyKinds = ["document type", "media type", "member type", "data type", "property", "group"];

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
    [Arguments("KCC Show When")]
    [Arguments("KCC Single Link")]
    [Arguments("KCC Links")]
    [Arguments("KCC Nav Items")]
    public async Task DataType_IsImportedOnFirstBoot(string name)
    {
        var dataTypes = Site.Services.GetRequiredService<IDataTypeService>();

        _ = await Assert.That(await dataTypes.GetAsync(name)).IsNotNull();
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
    public async Task SchemaKeys_AreUuidsTheBackofficeAccepts()
    {
        var keys = await SchemaKeysAsync();
        var rejected = keys
            .Where(entry => !BackofficeUuid.IsAccepted(entry.Key))
            .Select(entry => $"{entry.Kind} {entry.Owner} {entry.Key}");

        _ = await Assert.That(keys.Select(entry => entry.Kind).Distinct()).IsEquivalentTo(SchemaKeyKinds);
        _ = await Assert.That(string.Join(Environment.NewLine, rejected)).IsEmpty();
    }

    private static IEnumerable<(string Kind, string Owner, Guid Key)> TypeKeys(string kind, IContentTypeComposition type) =>
        type.PropertyGroups.Select(group => ("group", $"{type.Alias}/{group.Alias}", group.Key))
            .Concat(type.PropertyTypes
                .Where(property => !IsBuiltInMemberProperty(kind, property))
                .Select(property => ("property", $"{type.Alias}.{property.Alias}", property.Key)))
            .Prepend((kind, type.Alias, type.Key));

    // uSync pins Umbraco's built-in member properties to legacy int-based keys such as 2a280588-0000-… and applies
    // them whenever it imports the member type, so they are uSync's to choose. 17.7's backoffice validates content,
    // member and user keys, never a property type's.
    private static bool IsBuiltInMemberProperty(string kind, IPropertyType property) =>
        kind == "member type" && property.Alias.StartsWith("umbracoMember", StringComparison.Ordinal);

    private async Task<List<(string Kind, string Owner, Guid Key)>> SchemaKeysAsync()
    {
        var types = Site.Services.GetRequiredService<IContentTypeService>().GetAll().Select(type => TypeKeys("document type", type))
            .Concat(Site.Services.GetRequiredService<IMediaTypeService>().GetAll().Select(type => TypeKeys("media type", type)))
            .Concat(Site.Services.GetRequiredService<IMemberTypeService>().GetAll().Select(type => TypeKeys("member type", type)))
            .SelectMany(typeKeys => typeKeys);
        var dataTypes = await Site.Services.GetRequiredService<IDataTypeService>().GetAllAsync();

        return types.Concat(dataTypes.Select(dataType => ("data type", $"{dataType.Name}", dataType.Key))).ToList();
    }
}
