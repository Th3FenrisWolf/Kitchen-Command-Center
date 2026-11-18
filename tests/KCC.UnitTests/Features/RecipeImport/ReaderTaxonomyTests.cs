using KCC.Web.Features.RecipeImport;
using KCC.Web.Features.Recipes;

namespace KCC.UnitTests.Features.RecipeImport;

public class ReaderTaxonomyTests
{
    private static readonly CategoryChoice Breakfast = new(Guid.NewGuid(), "Breakfast");
    private static readonly CategoryChoice Dinner = new(Guid.NewGuid(), "Dinner");
    private static readonly TagChoice Vegetarian = new(Guid.NewGuid(), "Vegetarian", TagKinds.Diet);
    private static readonly TagChoice Spicy = new(Guid.NewGuid(), "Spicy", TagKinds.Style);
    private static readonly TagChoice KidsMenu = new(Guid.NewGuid(), "Kid's \"Pick\"", TagKinds.Style);

    private static readonly DetailsChoices Choices = new([Breakfast, Dinner], [Vegetarian, Spicy, KidsMenu]);

    [Test]
    public async Task TheRecipeFlow_WritesTheCategoryAndTagLines_AsTheContractShows()
    {
        var taxonomy = ReaderTaxonomy.From(Choices, ImportFlows.Recipe);

        _ = await Assert.That(taxonomy.Text).IsEqualTo(
            """
            The site's categories follow, one a line, each with its id and name. Choose at most one:
            {"id":"C1","name":"Breakfast"}
            {"id":"C2","name":"Dinner"}
            The site's tags follow, one a line, each with its id, name and group:
            {"id":"T1","name":"Vegetarian","group":"diet"}
            {"id":"T2","name":"Spicy","group":"style"}
            {"id":"T3","name":"Kid's \"Pick\"","group":"style"}
            """.ReplaceLineEndings("\n"));
    }

    [Test]
    public async Task TheVariantFlow_LeavesTheCategoriesOut()
    {
        var taxonomy = ReaderTaxonomy.From(Choices, ImportFlows.Variant);

        _ = await Assert.That(taxonomy.Text).IsEqualTo(
            """
            The site's tags follow, one a line, each with its id, name and group:
            {"id":"T1","name":"Vegetarian","group":"diet"}
            {"id":"T2","name":"Spicy","group":"style"}
            {"id":"T3","name":"Kid's \"Pick\"","group":"style"}
            """.ReplaceLineEndings("\n"));
        _ = await Assert.That(taxonomy.Category("C1")).IsNull();
    }

    [Test]
    public async Task ACategoryId_MapsBackToItsKey()
    {
        var taxonomy = ReaderTaxonomy.From(Choices, ImportFlows.Recipe);

        _ = await Assert.That(taxonomy.Category("C1")?.Key).IsEqualTo(Breakfast.Key);
        _ = await Assert.That(taxonomy.Category("C2")?.Key).IsEqualTo(Dinner.Key);
    }

    [Test]
    public async Task ATagId_MapsBackToItsKeyAndKind()
    {
        var taxonomy = ReaderTaxonomy.From(Choices, ImportFlows.Recipe);

        _ = await Assert.That(taxonomy.Tag("T1")).IsEqualTo(Vegetarian);
        _ = await Assert.That(taxonomy.Tag("T2")?.Kind).IsEqualTo(TagKinds.Style);
        _ = await Assert.That(taxonomy.Tag("T3")?.Key).IsEqualTo(KidsMenu.Key);
    }

    [Test]
    [Arguments("C3")]
    [Arguments("T1")]
    [Arguments("c1")]
    [Arguments("")]
    [Arguments((string)null)]
    public async Task AnUnknownCategoryId_MapsToNothing(string id)
    {
        _ = await Assert.That(ReaderTaxonomy.From(Choices, ImportFlows.Recipe).Category(id)).IsNull();
    }

    [Test]
    [Arguments("T4")]
    [Arguments("C1")]
    [Arguments("t1")]
    [Arguments("")]
    [Arguments((string)null)]
    public async Task AnUnknownTagId_MapsToNothing(string id)
    {
        _ = await Assert.That(ReaderTaxonomy.From(Choices, ImportFlows.Recipe).Tag(id)).IsNull();
    }

    [Test]
    public async Task ACategoryName_MapsToItsId_IgnoringCase()
    {
        var taxonomy = ReaderTaxonomy.From(Choices, ImportFlows.Recipe);

        _ = await Assert.That(taxonomy.CategoryId("dinner")).IsEqualTo("C2");
        _ = await Assert.That(taxonomy.CategoryId("Breakfast")).IsEqualTo("C1");
        _ = await Assert.That(taxonomy.CategoryId("Nope")).IsNull();
    }

    [Test]
    public async Task ATagName_MapsToItsId_IgnoringCase()
    {
        var taxonomy = ReaderTaxonomy.From(Choices, ImportFlows.Recipe);

        _ = await Assert.That(taxonomy.TagId("vegetarian")).IsEqualTo("T1");
        _ = await Assert.That(taxonomy.TagId("SPICY")).IsEqualTo("T2");
        _ = await Assert.That(taxonomy.TagId("Nope")).IsNull();
    }

    [Test]
    [Arguments(ImportFlows.Recipe)]
    [Arguments(ImportFlows.Variant)]
    public async Task Characters_CountTheText(string flow)
    {
        var taxonomy = ReaderTaxonomy.From(Choices, flow);

        _ = await Assert.That(taxonomy.Characters).IsEqualTo(taxonomy.Text.Length);
    }

    [Test]
    public async Task EmptyLists_WriteNoLines()
    {
        var taxonomy = ReaderTaxonomy.From(new DetailsChoices([], []), ImportFlows.Recipe);

        _ = await Assert.That(taxonomy.Text).IsEmpty();
        _ = await Assert.That(taxonomy.Characters).IsEqualTo(0);
    }
}
