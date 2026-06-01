using KCC.Web.Features.Pages.RecipeDetail;
using Xunit;

namespace KCC.Web.Tests.Features.Pages.RecipeDetail;

public class RecipeDetailViewModelTests
{
    [Fact]
    public void FromVariants_MapsVariantFieldsToSummary()
    {
        var viewModel = new RecipeDetailViewModel
        {
            RecipeName = "Mac & Cheese",
            RecipeDescription = "A classic comfort food",
            RecipeSlug = "/recipes/mac-and-cheese",
        };

        var variant = new VariantSummaryViewModel
        {
            Name = "Spicy Jalapeño",
            Description = "A kicked-up version",
            Guid = "/recipes/mac-and-cheese/spicy-jalapeno",
            Image = "/images/spicy.jpg",
            Tags = ["Spicy"],
        };

        viewModel.Variants.Add(variant);

        Assert.Single(viewModel.Variants);
        Assert.Equal("Spicy Jalapeño", viewModel.Variants[0].Name);
        Assert.Equal("/recipes/mac-and-cheese/spicy-jalapeno", viewModel.Variants[0].Slug);
    }
}
