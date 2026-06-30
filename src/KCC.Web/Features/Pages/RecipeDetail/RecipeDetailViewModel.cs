using KCC.Web.Features.Pages.Shared;

namespace KCC.Web.Features.Pages.RecipeDetail;

public class RecipeDetailViewModel : BasePageViewModel
{
    public string RecipeName { get; set; }
    public string RecipeDescription { get; set; }
    public string RecipeImagePath { get; set; }
    public string RecipeIcon { get; set; }
    public string RecipeCategory { get; set; }
    public Guid RecipeGuid { get; set; }
    public double RecipeAverageRating { get; set; }
    public int RecipeReviewCount { get; set; }
    public int RecipeTimesCooked { get; set; }
    public string AddVariantUrl { get; set; }
    public string StartedByName { get; set; }
    public IEnumerable<RecipeBreadcrumb> Breadcrumbs { get; set; } = [];
    public IEnumerable<VariantSummaryViewModel> Variants { get; set; } = [];
}

public record RecipeBreadcrumb(string LinkText, string Url);

public class VariantSummaryViewModel
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string Slug { get; set; }
    public string Image { get; set; }
    public string Icon { get; set; }
    public string AuthorName { get; set; }
    public IEnumerable<string> Tags { get; set; } = [];
    public int TotalTime { get; set; }
    public DateTime PublishedDate { get; set; }
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public int CookedCount { get; set; }
}
