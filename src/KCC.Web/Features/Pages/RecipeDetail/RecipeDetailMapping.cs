using KCC.Contributions;
using KCC.Web.Features.Providers;
using KCC.Web.Features.Recipes;

namespace KCC.Web.Features.Pages.RecipeDetail;

public static class RecipeDetailMapping
{
    public static IEnumerable<Guid> AuthorKeys(RecipePageData page) =>
        page.Variants.Select(variant => variant.AuthorKey).Prepend(page.Recipe.AuthorKey).OfType<Guid>();

    public static RecipeDetailViewModel Map(RecipePageData page, ContributionStats stats, IReadOnlyDictionary<Guid, string> authorNames)
    {
        var variantKeys = page.Variants.Select(variant => variant.Key).ToList();
        var rating = stats.RatingAcross(variantKeys);

        return new RecipeDetailViewModel
        {
            RecipeName = page.Recipe.Name,
            RecipeDescription = page.Recipe.Description,
            RecipeImagePath = page.Recipe.ImageUrl,
            RecipeIcon = page.Recipe.Icon,
            RecipeCategory = page.Recipe.Category,
            RecipeGuid = page.Recipe.Key,
            RecipeAverageRating = rating.Average,
            RecipeReviewCount = rating.Count,
            RecipeTimesCooked = stats.CookedAcross(variantKeys),
            AddVariantUrl = page.AddVariantUrl,
            StartedByName = AuthorNameProvider.NameFor(authorNames, page.Recipe.AuthorKey),
            Variants = page.Variants.Select(variant => Summary(variant, stats.For(variant.Key), authorNames)).ToList(),
        };
    }

    private static VariantSummaryViewModel Summary(VariantRecord variant, VariantStats stats, IReadOnlyDictionary<Guid, string> authorNames) => new()
    {
        Name = variant.Name,
        Description = variant.Description,
        Slug = variant.Url,
        Image = variant.ImageUrl,
        Icon = variant.Icon,
        AuthorName = AuthorNameProvider.NameFor(authorNames, variant.AuthorKey),
        Tags = variant.Tags,
        TotalTime = variant.TotalTime,
        PublishedDate = variant.CreateDate,
        AverageRating = stats.Rating.Average,
        ReviewCount = stats.Rating.Count,
        CookedCount = stats.CookedCount,
    };
}
