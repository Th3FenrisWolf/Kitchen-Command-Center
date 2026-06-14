using System;
using System.Collections.Generic;
using System.Linq;
using KCC.Web.Features.Pages.Account;
using Xunit;

namespace KCC.Web.Tests.Features.Pages.Account;

public class AccountViewModelTests
{
    private static readonly AccountViewModel.AuthoredRecipeInput MacAndCheese =
        new(PageId: 1, Name: "Mac & Cheese", Icon: "fa-pot", Url: "/recipes/mac-and-cheese", StartedByMe: true);

    private static readonly AccountViewModel.AuthoredRecipeInput Tacos =
        new(PageId: 2, Name: "Tacos", Icon: "fa-taco", Url: "/recipes/tacos", StartedByMe: false);

    [Fact]
    public void BuildRecipeGroups_GroupsVariantsUnderTheirRecipe()
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

        Assert.Equal(2, groups.Count);
        Assert.Equal("Mac & Cheese", groups[0].RecipeName);
        Assert.True(groups[0].StartedByYou);
        Assert.Single(groups[0].Variants);
        Assert.Equal("Classic", groups[0].Variants[0].Name);
        Assert.False(groups[1].StartedByYou);
    }

    [Fact]
    public void BuildRecipeGroups_MarksUnpublishedItemsPendingWithoutUrls()
    {
        var variants = new[]
        {
            new AccountViewModel.AuthoredVariantInput(PageId: 10, ParentPageId: 1, Name: "Classic", Icon: "fa-pot", Url: "/recipes/mac-and-cheese/classic"),
        };

        var groups = AccountViewModel.BuildRecipeGroups(
            [MacAndCheese], variants,
            publishedRecipeIds: new HashSet<int>(),
            publishedVariantIds: new HashSet<int>());

        Assert.True(groups[0].IsPending);
        Assert.Null(groups[0].RecipeUrl);
        Assert.True(groups[0].Variants[0].IsPending);
        Assert.Null(groups[0].Variants[0].Url);
    }

    [Fact]
    public void BuildRecipeGroups_PublishedItemsKeepUrlsAndAreNotPending()
    {
        var variants = new[]
        {
            new AccountViewModel.AuthoredVariantInput(PageId: 10, ParentPageId: 1, Name: "Classic", Icon: "fa-pot", Url: "/recipes/mac-and-cheese/classic"),
        };

        var groups = AccountViewModel.BuildRecipeGroups(
            [MacAndCheese], variants,
            publishedRecipeIds: new HashSet<int> { 1 },
            publishedVariantIds: new HashSet<int> { 10 });

        Assert.False(groups[0].IsPending);
        Assert.Equal("/recipes/mac-and-cheese", groups[0].RecipeUrl);
        Assert.False(groups[0].Variants[0].IsPending);
        Assert.Equal("/recipes/mac-and-cheese/classic", groups[0].Variants[0].Url);
    }

    [Fact]
    public void BuildRecipeGroups_KeepsStartedRecipesWithNoVariantsOfMine()
    {
        var groups = AccountViewModel.BuildRecipeGroups(
            [MacAndCheese], [],
            publishedRecipeIds: new HashSet<int> { 1 },
            publishedVariantIds: new HashSet<int>());

        Assert.Single(groups);
        Assert.Empty(groups[0].Variants);
    }

    [Fact]
    public void BuildRecipeGroups_DropsForeignRecipesWithNoVariantsAndSkipsOrphanVariants()
    {
        var orphan = new AccountViewModel.AuthoredVariantInput(PageId: 99, ParentPageId: 42, Name: "Orphan", Icon: "fa-x", Url: "/nowhere");

        var groups = AccountViewModel.BuildRecipeGroups(
            [Tacos], [orphan],
            publishedRecipeIds: new HashSet<int> { 2 },
            publishedVariantIds: new HashSet<int> { 99 });

        Assert.Empty(groups);
    }

    [Fact]
    public void BuildRecipeGroups_OrdersRecipesAndVariantsByName()
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

        Assert.Equal(new[] { "Mac & Cheese", "Tacos" }, groups.Select(group => group.RecipeName));
        Assert.Equal(new[] { "Classic", "Spicy" }, groups[0].Variants.Select(variant => variant.Name));
    }

    [Fact]
    public void BuildRecipeGroups_ReturnsEmptyForNoInputs()
    {
        var groups = AccountViewModel.BuildRecipeGroups([], [], new HashSet<int>(), new HashSet<int>());

        Assert.Empty(groups);
    }

    [Theory]
    [InlineData("Alex", "Carter", "AC")]
    [InlineData("alex", "carter", "AC")]
    [InlineData("Alex", "", "A")]
    [InlineData("  Alex  ", "  Carter  ", "AC")]
    public void ComputeInitials_UsesNames(string first, string last, string expected)
    {
        Assert.Equal(expected, AccountViewModel.ComputeInitials(first, last, "ignored"));
    }

    [Theory]
    [InlineData("", "", "alex.carter@example.com", "AL")]
    [InlineData(null, null, "a", "A")]
    [InlineData("", "", "", "")]
    public void ComputeInitials_FallsBackWhenNamesEmpty(string first, string last, string fallback, string expected)
    {
        Assert.Equal(expected, AccountViewModel.ComputeInitials(first, last, fallback));
    }

    [Fact]
    public void FormatMemberSince_FormatsMonthAndYear()
    {
        Assert.Equal("June 2024", AccountViewModel.FormatMemberSince(new DateTime(2024, 6, 15)));
    }

    [Fact]
    public void FormatMemberSince_ReturnsEmptyForNull()
    {
        Assert.Equal(string.Empty, AccountViewModel.FormatMemberSince(null));
    }
}
