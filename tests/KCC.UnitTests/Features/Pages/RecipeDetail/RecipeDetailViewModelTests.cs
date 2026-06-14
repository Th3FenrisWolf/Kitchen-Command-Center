using KCC.Web.Features.Pages.RecipeDetail;

namespace KCC.UnitTests.Features.Pages.RecipeDetail;

public class RecipeDetailViewModelTests
{
    [Test]
    public async Task Variants_HoldAssignedVariantSummaries()
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

        _ = await Assert.That(viewModel.Variants.Count()).IsEqualTo(1);
        var only = viewModel.Variants.Single();
        _ = await Assert.That(only.Name).IsEqualTo("Spicy Jalapeño");
        _ = await Assert.That(only.Slug).IsEqualTo("/recipes/mac-and-cheese/spicy-jalapeno");
        _ = await Assert.That(only.Image).IsEqualTo("/images/spicy.jpg");
        _ = await Assert.That(only.Tags.Count()).IsEqualTo(0);
    }

    [Test]
    public async Task VariantSummary_CarriesIcon()
    {
        var variant = new VariantSummaryViewModel
        {
            Name = "Spicy Jalapeño",
            Icon = "fa-duotone fa-pepper-hot",
        };

        _ = await Assert.That(variant.Icon).IsEqualTo("fa-duotone fa-pepper-hot");
    }
}
