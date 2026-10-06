using KCC.IntegrationTests.Config;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;
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
    [Arguments("card")]
    [Arguments("sectionSettings")]
    [Arguments("richTextBlock")]
    [Arguments("cardGridBlock")]
    [Arguments("stackerBlock")]
    [Arguments("navPreset")]
    [Arguments("navQuickLink")]
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
    [Arguments("KCC Section Background")]
    [Arguments("KCC Section Width")]
    [Arguments("KCC Wash")]
    [Arguments("KCC Card Columns")]
    [Arguments("KCC Rich Text")]
    [Arguments("KCC Optional Link")]
    [Arguments("KCC Cards")]
    [Arguments("KCC Home Sections")]
    [Arguments("KCC Tag Kind")]
    [Arguments("KCC Ramp")]
    [Arguments("KCC Nav Meals")]
    [Arguments("KCC Nav Diets")]
    [Arguments("KCC Nav Preset")]
    [Arguments("KCC Quick Picks")]
    [Arguments("KCC Search Suggestions")]
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
    public async Task RecipeTag_HasAMandatoryKind()
    {
        var kind = Site.Services.GetRequiredService<IContentTypeService>().Get("recipeTag")!.PropertyTypes.Single(property => property.Alias == "kind");
        var dataType = await Site.Services.GetRequiredService<IDataTypeService>().GetAsync(kind.DataTypeKey);

        _ = await Assert.That(kind.Mandatory).IsTrue();
        _ = await Assert.That(dataType!.Name).IsEqualTo("KCC Tag Kind");
    }

    [Test]
    public async Task RecipeCategory_HasAnOptionalIcon()
    {
        var icon = Site.Services.GetRequiredService<IContentTypeService>().Get("recipeCategory")!.PropertyTypes.Single(property => property.Alias == "icon");
        var dataType = await Site.Services.GetRequiredService<IDataTypeService>().GetAsync(icon.DataTypeKey);

        _ = await Assert.That(icon.Mandatory).IsFalse();
        _ = await Assert.That(dataType!.Name).IsEqualTo("KCC Recipe Icon");
    }

    [Test]
    public async Task Member_HasAnOptionalRamp()
    {
        var ramp = Site.Services.GetRequiredService<IMemberTypeService>().Get("Member")!.PropertyTypes.Single(property => property.Alias == "ramp");
        var dataType = await Site.Services.GetRequiredService<IDataTypeService>().GetAsync(ramp.DataTypeKey);

        _ = await Assert.That(ramp.Mandatory).IsFalse();
        _ = await Assert.That(dataType!.Name).IsEqualTo("KCC Ramp");
    }

    [Test]
    public async Task NavElements_AreElementTypes()
    {
        var contentTypes = Site.Services.GetRequiredService<IContentTypeService>();

        _ = await Assert.That(contentTypes.Get("navLink")!.IsElement).IsTrue();
        _ = await Assert.That(contentTypes.Get("navGroup")!.IsElement).IsTrue();
    }

    [Test]
    [Arguments("card")]
    [Arguments("sectionSettings")]
    [Arguments("richTextBlock")]
    [Arguments("cardGridBlock")]
    [Arguments("stackerBlock")]
    public async Task HomeBlock_IsAnElementType(string alias)
    {
        _ = await Assert.That(Site.Services.GetRequiredService<IContentTypeService>().Get(alias)!.IsElement).IsTrue();
    }

    [Test]
    public async Task HomeSections_OfferTheThreeBlocks_EachWithSectionSettings()
    {
        var contentTypes = Site.Services.GetRequiredService<IContentTypeService>();
        var home = contentTypes.Get("homePage")!;
        var sections = home.PropertyTypes.Single(property => property.Alias == "sections");
        var dataType = await Site.Services.GetRequiredService<IDataTypeService>().GetAsync(sections.DataTypeKey);
        var blocks = ((BlockListConfiguration)dataType!.ConfigurationObject!).Blocks;

        _ = await Assert.That(dataType.Name).IsEqualTo("KCC Home Sections");
        _ = await Assert.That(blocks.Select(block => contentTypes.Get(block.ContentElementTypeKey)!.Alias))
            .IsEquivalentTo(["richTextBlock", "cardGridBlock", "stackerBlock"]);
        _ = await Assert.That(blocks.All(block => block.SettingsElementTypeKey == contentTypes.Get("sectionSettings")!.Key)).IsTrue();
    }

    [Test]
    [Arguments("navMeals", "KCC Nav Meals")]
    [Arguments("navDiets", "KCC Nav Diets")]
    [Arguments("navQuickPicks", "KCC Quick Picks")]
    [Arguments("navSearchSuggestions", "KCC Search Suggestions")]
    [Arguments("navRecipesNote", "Textstring")]
    public async Task SiteSettings_HoldTheNavsContent_InTheNavigationGroup(string alias, string dataTypeName)
    {
        var settings = Site.Services.GetRequiredService<IContentTypeService>().Get("siteSettings")!;
        var property = settings.PropertyGroups.Single(group => group.Alias == "navigation").PropertyTypes!.Single(type => type.Alias == alias);
        var dataType = await Site.Services.GetRequiredService<IDataTypeService>().GetAsync(property.DataTypeKey);

        _ = await Assert.That(property.Mandatory).IsFalse();
        _ = await Assert.That(dataType!.Name).IsEqualTo(dataTypeName);
    }

    [Test]
    [Arguments("KCC Nav Meals", "recipeCategory")]
    [Arguments("KCC Nav Diets", "recipeTag")]
    public async Task NavPicker_OffersOnlyItsTaxonomy_AsManyAsWanted(string dataTypeName, string contentTypeAlias)
    {
        var dataType = await Site.Services.GetRequiredService<IDataTypeService>().GetAsync(dataTypeName);
        var configuration = (MultiNodePickerConfiguration)dataType!.ConfigurationObject!;
        var allowed = Site.Services.GetRequiredService<IContentTypeService>().Get(contentTypeAlias)!.Key;

        _ = await Assert.That(configuration.Filter).IsEqualTo(allowed.ToString());
        _ = await Assert.That(configuration.MaxNumber).IsEqualTo(0);
    }

    [Test]
    public async Task QuickPicks_OfferPresetsAndQuickLinks()
    {
        var contentTypes = Site.Services.GetRequiredService<IContentTypeService>();
        var dataType = await Site.Services.GetRequiredService<IDataTypeService>().GetAsync("KCC Quick Picks");
        var elements = ((BlockListConfiguration)dataType!.ConfigurationObject!).Blocks.Select(block => contentTypes.Get(block.ContentElementTypeKey)!).ToList();

        _ = await Assert.That(elements.Select(element => element.Alias)).IsEquivalentTo(["navPreset", "navQuickLink"]);
        _ = await Assert.That(elements.All(element => element.IsElement)).IsTrue();
    }

    [Test]
    [Arguments("navPreset", "preset", true)]
    [Arguments("navPreset", "label", false)]
    [Arguments("navQuickLink", "label", true)]
    [Arguments("navQuickLink", "link", true)]
    public async Task QuickPickProperty_IsMandatoryOnlyWhereThePickNeedsIt(string elementAlias, string propertyAlias, bool mandatory)
    {
        var element = Site.Services.GetRequiredService<IContentTypeService>().Get(elementAlias)!;

        _ = await Assert.That(element.PropertyTypes.Single(property => property.Alias == propertyAlias).Mandatory).IsEqualTo(mandatory);
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
