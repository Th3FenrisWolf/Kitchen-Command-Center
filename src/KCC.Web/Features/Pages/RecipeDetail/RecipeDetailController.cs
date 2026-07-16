using CMS.ContentEngine;
using CMS.Websites;
using KCC;
using KCC.Contributions.Data;
using KCC.ResourceStrings.Data;
using KCC.Web.Features.Components.Breadcrumbs;
using KCC.Web.Features.Members;
using KCC.Web.Features.Models.Constants;
using KCC.Web.Features.Pages.RecipeDetail;
using KCC.Web.Features.Pages.Shared;
using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using Microsoft.AspNetCore.Mvc;

[assembly: RegisterWebPageRoute(
    Recipe.CONTENT_TYPE_NAME,
    typeof(RecipeDetailController),
    WebsiteChannelNames = [XperienceConstants.WebsiteChannelName]
)]

namespace KCC.Web.Features.Pages.RecipeDetail;

public class RecipeDetailController(
    IWebPageDataContextRetriever webPageDataContextRetriever,
    IContentRetriever contentRetriever,
    ITaxonomyRetriever taxonomyRetriever,
    IPreferredLanguageRetriever preferredLanguageRetriever,
    IAuthorNameResolver authorNameResolver,
    IResourceStringInfoProvider resourceStrings,
    IVariantReviewInfoProvider reviewProvider,
    IVariantCookedInfoProvider cookedProvider,
    BreadcrumbService breadcrumbService
) : Controller
{
    public async Task<IActionResult> Index()
    {
        var language = preferredLanguageRetriever.Get();
        var pageId = webPageDataContextRetriever.Retrieve().WebPage.WebPageItemID;

        var recipe = (await contentRetriever.RetrievePages<Recipe>(
            new() { LinkedItemsMaxLevel = 1 },
            query => query
                .Where(where => where
                    .WhereEquals(nameof(IWebPageFieldsSource.SystemFields.WebPageItemID), pageId))
                .TopN(1),
            new($"{nameof(RecipeDetailController)}|{nameof(Index)}|{pageId}")
        )).FirstOrDefault();

        if (recipe is null)
        {
            return NotFound();
        }

        var addVariantPage = (await contentRetriever.RetrievePages<AddVariantPage>(
            new(),
            query => query.TopN(1),
            new($"{nameof(RecipeDetailController)}|{nameof(AddVariantPage)}")
        )).FirstOrDefault();

        var categoryId = recipe.Categories?.FirstOrDefault()?.Identifier ?? Guid.Empty;
        var resolvedCategory = (await taxonomyRetriever.RetrieveTags([categoryId], language)).FirstOrDefault();

        var recipeGuid = recipe.SystemFields.ContentItemGUID;
        var recipeRating = reviewProvider.GetRecipeAverage(recipeGuid);
        var recipeTimesCooked = cookedProvider.GetRecipeCookedCount(recipeGuid);

        var viewModel = new RecipeDetailViewModel
        {
            RecipeName = recipe.Name,
            RecipeDescription = recipe.Description,
            RecipeImagePath = recipe.Image?.FirstOrDefault()?.Asset?.Url,
            RecipeIcon = recipe.Icon,
            RecipeCategory = resolvedCategory?.Name,
            RecipeGuid = recipeGuid,
            RecipeAverageRating = recipeRating.Average,
            RecipeReviewCount = recipeRating.Count,
            RecipeTimesCooked = recipeTimesCooked,
            AddVariantUrl = addVariantPage?.GetUrl().RelativePath,
            Variants = await RetrieveVariants(pageId, language),
            StartedByName = await authorNameResolver.Resolve(recipe.AuthorMemberGuid),
            Breadcrumbs = (await breadcrumbService.BuildBreadcrumbsAsync(pageId)).Select(b => new RecipeBreadcrumb(b.LinkText, b.Url)),
            ResourceStrings = GetStrings(),
        };

        await recipe.MapMetadata(viewModel);
        return View("~/Features/Pages/RecipeDetail/Index.cshtml", viewModel);
    }

    private async Task<IEnumerable<VariantSummaryViewModel>> RetrieveVariants(int pageId, string language)
    {
        var variants = await contentRetriever.RetrievePages<RecipeVariant>(
            new() { LinkedItemsMaxLevel = 1 },
            query => query.Where(where => where
                .WhereEquals(nameof(IWebPageFieldsSource.SystemFields.WebPageItemParentID), pageId)),
            new($"{nameof(RecipeDetailController)}|{nameof(RetrieveVariants)}|{pageId}")
        );

        var variantGuids = variants.Select(v => v.SystemFields.ContentItemGUID).ToArray();
        var averages = reviewProvider.GetAveragesForVariants(variantGuids);
        var cookedCounts = cookedProvider.GetCookedCountsForVariants(variantGuids);

        var tagIds = variants
            .SelectMany(v => v.Tags?.Select(tag => tag.Identifier) ?? [])
            .Distinct();

        var resolvedTags = await taxonomyRetriever.RetrieveTags(tagIds, language);
        var authorNames = await authorNameResolver.ResolveMany(variants.Select(variant => variant.AuthorMemberGuid));

        return variants.Select(variant => new VariantSummaryViewModel
        {
            Name = variant.Name,
            Description = variant.Description,
            Slug = variant.GetUrl().RelativePath,
            Image = variant.Images?.FirstOrDefault()?.Asset?.Url,
            Icon = variant.Icon,
            AuthorName = authorNames.GetValueOrDefault(variant.AuthorMemberGuid),
            TotalTime = variant.PrepTime + variant.CookTime,
            PublishedDate = variant.MetadataPublishDate,
            AverageRating = averages.GetValueOrDefault(variant.SystemFields.ContentItemGUID).Average,
            ReviewCount = averages.GetValueOrDefault(variant.SystemFields.ContentItemGUID).Count,
            CookedCount = cookedCounts.GetValueOrDefault(variant.SystemFields.ContentItemGUID),
            Tags = resolvedTags?
                .IntersectBy(variant.Tags?.Select(tag => tag.Identifier) ?? [], resolved => resolved.Identifier)
                .Select(tag => tag.Title) ?? [],
        });
    }

    private Dictionary<string, string> GetStrings() => resourceStrings.GetManyOrDefault(
        "RecipeDetail.AddVariant",
        "RecipeDetail.StartedBy",
        "RecipeDetail.Variants",
        "RecipeDetail.By",
        "RecipeDetail.ComingSoon",
        "RecipeDetail.AvgTime",
        "RecipeDetail.Fastest",
        "RecipeDetail.Contributors",
        "RecipeDetail.TopVariant",
        "RecipeDetail.RankingComingSoon",
        "RecipeDetail.AllVariants",
        "RecipeDetail.Sort",
        "RecipeDetail.SortNewest",
        "RecipeDetail.SortFastest",
        "RecipeDetail.SortTopRated",
        "RecipeDetail.Min",
        "RecipeDetail.SearchVariants",
        "RecipeDetail.Grid",
        "RecipeDetail.List",
        "RecipeDetail.Total",
        "RecipeDetail.Of",
        "RecipeDetail.NoVariantsMatch",
        "RecipeDetail.TryDifferentFilter",
        "RecipeDetail.ClearFilters",
        "RecipeDetail.TimesCooked",
        "RecipeDetail.NoRatingsYet",
        "RecipeDetail.Rating",
        "RecipeDetail.Reviews",
        "RecipeDetail.All"
    );
}
