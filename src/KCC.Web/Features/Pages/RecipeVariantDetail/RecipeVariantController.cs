using CMS.ContentEngine;
using CMS.Websites;
using CMS.Websites.Routing;
using KCC;
using KCC.Web.Features.Models.Constants;
using KCC.Web.Features.Pages.RecipeVariantDetail;
using KCC.Web.Features.Pages.Shared;
using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using Microsoft.AspNetCore.Mvc;

[assembly: RegisterWebPageRoute(
    RecipeVariant.CONTENT_TYPE_NAME,
    typeof(RecipeVariantController),
    WebsiteChannelNames = [XperienceConstants.WebsiteChannelName]
)]

namespace KCC.Web.Features.Pages.RecipeVariantDetail;

public class RecipeVariantController(
    IWebPageDataContextRetriever webPageDataContextRetriever,
    IContentRetriever contentRetriever,
    ITaxonomyRetriever taxonomyRetriever,
    IPreferredLanguageRetriever preferredLanguageRetriever
) : Controller
{
    public async Task<IActionResult> Index()
    {
        var context = webPageDataContextRetriever.Retrieve().WebPage;
        var pageId = context.WebPageItemID;

        var variant = (await contentRetriever.RetrievePages<RecipeVariant>(
            new() { LinkedItemsMaxLevel = 1 },
            query => query
                .Where(w => w.WhereEquals(nameof(IWebPageFieldsSource.SystemFields.WebPageItemID), pageId))
                .TopN(1),
            new($"{nameof(RecipeVariantController)}|{nameof(Index)}|{pageId}")
        )).FirstOrDefault();

        if (variant is null)
        {
            return NotFound();
        }

        var parentId = variant.SystemFields.WebPageItemParentID;

        var parentRecipe = (await contentRetriever.RetrievePages<Recipe>(
            new() { LinkedItemsMaxLevel = 1 },
            query => query
                .Where(w => w.WhereEquals(nameof(IWebPageFieldsSource.SystemFields.WebPageItemID), parentId))
                .TopN(1),
            new($"{nameof(RecipeVariantController)}|Parent|{parentId}")
        )).FirstOrDefault();

        var siblings = await contentRetriever.RetrievePages<RecipeVariant>(
            new(),
            query => query
                .Where(w => w.WhereEquals(nameof(IWebPageFieldsSource.SystemFields.WebPageItemParentID), parentId))
                .Where(w => w.WhereNotEquals(nameof(IWebPageFieldsSource.SystemFields.WebPageItemID), pageId)),
            new($"{nameof(RecipeVariantController)}|Siblings|{parentId}|{pageId}")
        );

        var language = preferredLanguageRetriever.Get();
        var tagGuids = variant.Tags?.Select(t => t.Identifier) ?? Enumerable.Empty<Guid>();
        var tagResult = await taxonomyRetriever.RetrieveTags(tagGuids, language);
        var resolvedTags = tagResult?.Select(t => t.Title).ToList() ?? [];

        var viewModel = new RecipeVariantViewModel
        {
            VariantName = variant.Name,
            VariantDescription = variant.Description,
            ImagePaths = variant.Images?
                .Select(i => i.Asset?.Url)
                .Where(u => u is not null)
                .ToList() ?? [],
            PrepTime = variant.PrepTime > 0 ? variant.PrepTime : null,
            CookTime = variant.CookTime > 0 ? variant.CookTime : null,
            Servings = variant.ServingNumber > 0 ? variant.ServingNumber : null,
            Tags = resolvedTags,
            Ingredients = RecipeVariantViewModel.DeserializeIngredients(variant.Ingredients),
            Instructions = RecipeVariantViewModel.DeserializeInstructions(variant.Instructions),
            VariantSlug = variant.GetUrl().RelativePath,
            RecipeName = parentRecipe?.Name ?? string.Empty,
            RecipeSlug = parentRecipe?.GetUrl().RelativePath ?? string.Empty,
            SiblingVariants = siblings.Select(s => new SiblingVariantViewModel
            {
                Name = s.Name,
                Slug = s.GetUrl().RelativePath,
            }).ToList(),
        };
        variant.MapMetadata(viewModel);
        variant.MapWebPageFields(viewModel);

        return View("~/Features/Pages/RecipeVariantDetail/Index.cshtml", viewModel);
    }
}
