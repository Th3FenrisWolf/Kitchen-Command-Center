using KCC.Web.Features.Pages.RecipeDetail;
using Xunit;

namespace KCC.Web.Tests.Features.Pages.RecipeDetail;

public class RecipeDetailViewModelTests
{
    [Fact]
    public void Variants_HoldAssignedVariantSummaries()
    {
        var variant = new VariantSummaryViewModel
        {
            Name = "Spicy Jalapeño",
            Description = "A kicked-up version",
            Slug = "/recipes/mac-and-cheese/spicy-jalapeno",
            Image = "/images/spicy.jpg",
        };

        var viewModel = new RecipeDetailViewModel
        {
            RecipeName = "Mac & Cheese",
            RecipeDescription = "A classic comfort food",
            Variants = [variant],
        };

        var only = Assert.Single(viewModel.Variants);
        Assert.Equal("Spicy Jalapeño", only.Name);
        Assert.Equal("/recipes/mac-and-cheese/spicy-jalapeno", only.Slug);
        Assert.Equal("/images/spicy.jpg", only.Image);
        Assert.Empty(only.Tags);
    }

    [Fact]
    public void VariantSummary_CarriesIcon()
    {
        var variant = new VariantSummaryViewModel
        {
            Name = "Spicy Jalapeño",
            Icon = "fa-duotone fa-pepper-hot",
        };

        Assert.Equal("fa-duotone fa-pepper-hot", variant.Icon);
    }
}
