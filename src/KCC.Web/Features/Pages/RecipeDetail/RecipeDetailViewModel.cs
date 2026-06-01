using KCC.Web.Features.Pages.Shared;

namespace KCC.Web.Features.Pages.RecipeDetail;

public class RecipeDetailViewModel : BasePageViewModel
{
    public string RecipeName { get; set; }
    public string RecipeDescription { get; set; }
    public string RecipeImagePath { get; set; }
    public CMS.ContentEngine.Tag RecipeCategory { get; set; }
    public Guid RecipeGuid { get; set; }
    public string AddVariantUrl { get; set; }
    public IEnumerable<VariantSummaryViewModel> Variants { get; set; } = [];
}

public class VariantSummaryViewModel
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string Slug { get; set; }
    public string Image { get; set; }
    public IEnumerable<CMS.ContentEngine.Tag> Tags { get; set; } = [];
}
