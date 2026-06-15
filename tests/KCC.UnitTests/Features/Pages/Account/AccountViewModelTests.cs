using System;
using System.Collections.Generic;
using System.Linq;
using KCC.Web.Features.Pages.Account;

namespace KCC.UnitTests.Features.Pages.Account;

public class AccountViewModelTests
{
    private static readonly AccountViewModel.AuthoredRecipeInput MacAndCheese =
        new(PageId: 1, Name: "Mac & Cheese", Icon: "fa-pot", Url: "/recipes/mac-and-cheese", StartedByMe: true);

    private static readonly AccountViewModel.AuthoredRecipeInput Tacos =
        new(PageId: 2, Name: "Tacos", Icon: "fa-taco", Url: "/recipes/tacos", StartedByMe: false);

    [Test]
    public async Task BuildRecipeGroups_GroupsVariantsUnderTheirRecipe()
    {
        var variants = new[]
        {
            new AccountViewModel.AuthoredVariantInput(PageId: 10, ParentPageId: 1, Name: "Classic", Icon: "fa-pot", Url: "/recipes/mac-and-cheese/classic"),
            new AccountViewModel.AuthoredVariantInput(PageId: 11, ParentPageId: 2, Name: "Carnitas", Icon: "fa-taco", Url: "/recipes/tacos/carnitas"),
        };

        var groups = AccountViewModel.BuildRecipeGroups(
            [MacAndCheese, Tacos], variants,
            publishedRecipeIds: new HashSet<int> { 1, 2 },
            publishedVariantIds: new HashSet<int> { 10, 11 });

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
            new AccountViewModel.AuthoredVariantInput(PageId: 10, ParentPageId: 1, Name: "Classic", Icon: "fa-pot", Url: "/recipes/mac-and-cheese/classic"),
        };

        var groups = AccountViewModel.BuildRecipeGroups(
            [MacAndCheese], variants,
            publishedRecipeIds: new HashSet<int>(),
            publishedVariantIds: new HashSet<int>());

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
            new AccountViewModel.AuthoredVariantInput(PageId: 10, ParentPageId: 1, Name: "Classic", Icon: "fa-pot", Url: "/recipes/mac-and-cheese/classic"),
        };

        var groups = AccountViewModel.BuildRecipeGroups(
            [MacAndCheese], variants,
            publishedRecipeIds: new HashSet<int> { 1 },
            publishedVariantIds: new HashSet<int> { 10 });

        _ = await Assert.That(groups.ElementAt(0).IsPending).IsFalse();
        _ = await Assert.That(groups.ElementAt(0).RecipeUrl).IsEqualTo("/recipes/mac-and-cheese");
        _ = await Assert.That(groups.ElementAt(0).Variants.ElementAt(0).IsPending).IsFalse();
        _ = await Assert.That(groups.ElementAt(0).Variants.ElementAt(0).Url).IsEqualTo("/recipes/mac-and-cheese/classic");
    }

    [Test]
    public async Task BuildRecipeGroups_KeepsStartedRecipesWithNoVariantsOfMine()
    {
        var groups = AccountViewModel.BuildRecipeGroups(
            [MacAndCheese], [],
            publishedRecipeIds: new HashSet<int> { 1 },
            publishedVariantIds: new HashSet<int>());

        _ = await Assert.That(groups.Count()).IsEqualTo(1);
        _ = await Assert.That(groups.ElementAt(0).Variants.Count()).IsEqualTo(0);
    }

    [Test]
    public async Task BuildRecipeGroups_DropsForeignRecipesWithNoVariantsAndSkipsOrphanVariants()
    {
        var orphan = new AccountViewModel.AuthoredVariantInput(PageId: 99, ParentPageId: 42, Name: "Orphan", Icon: "fa-x", Url: "/nowhere");

        var groups = AccountViewModel.BuildRecipeGroups(
            [Tacos], [orphan],
            publishedRecipeIds: new HashSet<int> { 2 },
            publishedVariantIds: new HashSet<int> { 99 });

        _ = await Assert.That(groups.Count()).IsEqualTo(0);
    }

    [Test]
    public async Task BuildRecipeGroups_OrdersRecipesAndVariantsByName()
    {
        var variants = new[]
        {
            new AccountViewModel.AuthoredVariantInput(PageId: 12, ParentPageId: 1, Name: "Spicy", Icon: "fa-pot", Url: "/b"),
            new AccountViewModel.AuthoredVariantInput(PageId: 10, ParentPageId: 1, Name: "Classic", Icon: "fa-pot", Url: "/a"),
        };

        var groups = AccountViewModel.BuildRecipeGroups(
            [Tacos with { StartedByMe = true }, MacAndCheese], variants,
            publishedRecipeIds: new HashSet<int> { 1, 2 },
            publishedVariantIds: new HashSet<int> { 10, 12 });

        _ = await Assert.That(groups.Select(group => group.RecipeName)).IsEquivalentTo(new[] { "Mac & Cheese", "Tacos" });
        _ = await Assert.That(groups.ElementAt(0).Variants.Select(variant => variant.Name)).IsEquivalentTo(new[] { "Classic", "Spicy" });
    }

    [Test]
    public async Task BuildRecipeGroups_ReturnsEmptyForNoInputs()
    {
        var groups = AccountViewModel.BuildRecipeGroups([], [], new HashSet<int>(), new HashSet<int>());

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
