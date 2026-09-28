using KCC.Web.Features.Pages.Account;

namespace KCC.UnitTests.Features.Pages.Account;

public class AccountViewModelTests
{
    private static readonly Guid MacAndCheeseKey = Guid.NewGuid();
    private static readonly Guid TacosKey = Guid.NewGuid();
    private static readonly Guid ClassicKey = Guid.NewGuid();
    private static readonly Guid CarnitasKey = Guid.NewGuid();
    private static readonly Guid SpicyKey = Guid.NewGuid();

    private static readonly AccountViewModel.AuthoredRecipeInput MacAndCheese =
        new(Key: MacAndCheeseKey, Name: "Mac & Cheese", Icon: "fa-pot", Url: "/recipes/mac-and-cheese", StartedByMe: true);

    private static readonly AccountViewModel.AuthoredRecipeInput Tacos =
        new(Key: TacosKey, Name: "Tacos", Icon: "fa-taco", Url: "/recipes/tacos", StartedByMe: false);

    [Test]
    public async Task BuildRecipeGroups_GroupsVariantsUnderTheirRecipe()
    {
        var variants = new[]
        {
            new AccountViewModel.AuthoredVariantInput(Key: ClassicKey, ParentKey: MacAndCheeseKey, Name: "Classic", Icon: "fa-pot", Url: "/recipes/mac-and-cheese/classic"),
            new AccountViewModel.AuthoredVariantInput(Key: CarnitasKey, ParentKey: TacosKey, Name: "Carnitas", Icon: "fa-taco", Url: "/recipes/tacos/carnitas"),
        };

        var groups = AccountViewModel.BuildRecipeGroups(
            [MacAndCheese, Tacos],
            variants,
            publishedRecipeKeys: new HashSet<Guid> { MacAndCheeseKey, TacosKey },
            publishedVariantKeys: new HashSet<Guid> { ClassicKey, CarnitasKey });

        _ = await Assert.That(groups.Count()).IsEqualTo(2);
        _ = await Assert.That(groups.ElementAt(0).RecipeName).IsEqualTo("Mac & Cheese");
        _ = await Assert.That(groups.ElementAt(0).StartedByYou).IsTrue();
        _ = await Assert.That(groups.ElementAt(0).Variants.Count()).IsEqualTo(1);
        _ = await Assert.That(groups.ElementAt(0).Variants.ElementAt(0).Name).IsEqualTo("Classic");
        _ = await Assert.That(groups.ElementAt(1).StartedByYou).IsFalse();
    }

    [Test]
    public async Task BuildRecipeGroups_MarksUnpublishedItemsPendingWithoutUrls()
    {
        var variants = new[]
        {
            new AccountViewModel.AuthoredVariantInput(Key: ClassicKey, ParentKey: MacAndCheeseKey, Name: "Classic", Icon: "fa-pot", Url: "/recipes/mac-and-cheese/classic"),
        };

        var groups = AccountViewModel.BuildRecipeGroups([MacAndCheese], variants, new HashSet<Guid>(), new HashSet<Guid>());

        _ = await Assert.That(groups.ElementAt(0).IsPending).IsTrue();
        _ = await Assert.That(groups.ElementAt(0).RecipeUrl).IsNull();
        _ = await Assert.That(groups.ElementAt(0).Variants.ElementAt(0).IsPending).IsTrue();
        _ = await Assert.That(groups.ElementAt(0).Variants.ElementAt(0).Url).IsNull();
    }

    [Test]
    public async Task BuildRecipeGroups_PublishedItemsKeepUrlsAndAreNotPending()
    {
        var variants = new[]
        {
            new AccountViewModel.AuthoredVariantInput(Key: ClassicKey, ParentKey: MacAndCheeseKey, Name: "Classic", Icon: "fa-pot", Url: "/recipes/mac-and-cheese/classic"),
        };

        var groups = AccountViewModel.BuildRecipeGroups(
            [MacAndCheese],
            variants,
            publishedRecipeKeys: new HashSet<Guid> { MacAndCheeseKey },
            publishedVariantKeys: new HashSet<Guid> { ClassicKey });

        _ = await Assert.That(groups.ElementAt(0).IsPending).IsFalse();
        _ = await Assert.That(groups.ElementAt(0).RecipeUrl).IsEqualTo("/recipes/mac-and-cheese");
        _ = await Assert.That(groups.ElementAt(0).Variants.ElementAt(0).IsPending).IsFalse();
        _ = await Assert.That(groups.ElementAt(0).Variants.ElementAt(0).Url).IsEqualTo("/recipes/mac-and-cheese/classic");
    }

    [Test]
    public async Task BuildRecipeGroups_KeepsStartedRecipesWithNoVariantsOfMine()
    {
        var groups = AccountViewModel.BuildRecipeGroups([MacAndCheese], [], new HashSet<Guid> { MacAndCheeseKey }, new HashSet<Guid>());

        _ = await Assert.That(groups.Count()).IsEqualTo(1);
        _ = await Assert.That(groups.ElementAt(0).Variants.Count()).IsEqualTo(0);
    }

    [Test]
    public async Task BuildRecipeGroups_DropsForeignRecipesWithNoVariantsAndSkipsOrphanVariants()
    {
        var orphan = new AccountViewModel.AuthoredVariantInput(Key: Guid.NewGuid(), ParentKey: Guid.NewGuid(), Name: "Orphan", Icon: "fa-x", Url: "/nowhere");

        var groups = AccountViewModel.BuildRecipeGroups([Tacos], [orphan], new HashSet<Guid> { TacosKey }, new HashSet<Guid> { orphan.Key });

        _ = await Assert.That(groups.Count()).IsEqualTo(0);
    }

    [Test]
    public async Task BuildRecipeGroups_OrdersRecipesAndVariantsByName()
    {
        var variants = new[]
        {
            new AccountViewModel.AuthoredVariantInput(Key: SpicyKey, ParentKey: MacAndCheeseKey, Name: "Spicy", Icon: "fa-pot", Url: "/b"),
            new AccountViewModel.AuthoredVariantInput(Key: ClassicKey, ParentKey: MacAndCheeseKey, Name: "Classic", Icon: "fa-pot", Url: "/a"),
        };

        var groups = AccountViewModel.BuildRecipeGroups(
            [Tacos with { StartedByMe = true }, MacAndCheese],
            variants,
            publishedRecipeKeys: new HashSet<Guid> { MacAndCheeseKey, TacosKey },
            publishedVariantKeys: new HashSet<Guid> { ClassicKey, SpicyKey });

        _ = await Assert.That(groups.Select(group => group.RecipeName)).IsEquivalentTo(new[] { "Mac & Cheese", "Tacos" });
        _ = await Assert.That(groups.ElementAt(0).Variants.Select(variant => variant.Name)).IsEquivalentTo(new[] { "Classic", "Spicy" });
    }

    [Test]
    public async Task BuildRecipeGroups_ReturnsEmptyForNoInputs()
    {
        var groups = AccountViewModel.BuildRecipeGroups([], [], new HashSet<Guid>(), new HashSet<Guid>());

        _ = await Assert.That(groups.Count()).IsEqualTo(0);
    }

    [Test]
    [Arguments("Alex", "Carter", "AC")]
    [Arguments("alex", "carter", "AC")]
    [Arguments("Alex", "", "A")]
    [Arguments("  Alex  ", "  Carter  ", "AC")]
    public async Task ComputeInitials_UsesNames(string first, string last, string expected)
    {
        _ = await Assert.That(AccountViewModel.ComputeInitials(first, last, "ignored")).IsEqualTo(expected);
    }

    [Test]
    [Arguments("", "", "alex.carter@example.com", "AL")]
    [Arguments(null, null, "a", "A")]
    [Arguments("", "", "", "")]
    public async Task ComputeInitials_FallsBackWhenNamesEmpty(string first, string last, string fallback, string expected)
    {
        _ = await Assert.That(AccountViewModel.ComputeInitials(first, last, fallback)).IsEqualTo(expected);
    }

    [Test]
    public async Task FormatMemberSince_FormatsMonthAndYear()
    {
        _ = await Assert.That(AccountViewModel.FormatMemberSince(new DateTime(2024, 6, 15))).IsEqualTo("June 2024");
    }

    [Test]
    public async Task FormatMemberSince_ReturnsEmptyForNull()
    {
        _ = await Assert.That(AccountViewModel.FormatMemberSince(null)).IsEqualTo(string.Empty);
    }
}
