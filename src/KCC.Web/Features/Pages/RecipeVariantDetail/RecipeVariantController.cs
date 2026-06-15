using CMS.ContentEngine;
using CMS.Websites;
using KCC;
using KCC.ResourceStrings.Data;
using KCC.Web.Features.Extensions;
using KCC.Web.Features.Helpers;
using KCC.Web.Features.Members;
using KCC.Web.Features.Models.Common;
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
    IPreferredLanguageRetriever preferredLanguageRetriever,
    IAuthorNameResolver authorNameResolver,
    IResourceStringInfoProvider resourceStrings
) : Controller
{
    public async Task<IActionResult> Index()
    {
        var pageId = webPageDataContextRetriever.Retrieve().WebPage.WebPageItemID;
        var variantPage = await contentRetriever.RetrievePage<RecipeVariant>(pageId, linkedItemsMaxLevel: 1);

        if (variantPage is null)
        {
            return NotFound();
        }

        var parentId = variantPage.SystemFields.WebPageItemParentID;
        var recipePage = await contentRetriever.RetrievePage<Recipe>(parentId);

        if (recipePage is null)
        {
            return NotFound();
        }

        var siblings = await contentRetriever.RetrievePages<RecipeVariant>(
            new(),
            query => query.Where(w => w
                .WhereEquals(nameof(IWebPageFieldsSource.SystemFields.WebPageItemParentID), parentId)
                .WhereNotEquals(nameof(IWebPageFieldsSource.SystemFields.WebPageItemID), pageId)
            ),
            new($"{nameof(RecipeVariantController)}|{parentId}|{pageId}|Siblings")
        );

        var language = preferredLanguageRetriever.Get();
        var tagGuids = variantPage.Tags?.Select(t => t.Identifier) ?? [];
        var tagResult = await taxonomyRetriever.RetrieveTags(tagGuids, language);
        var resolvedTags = tagResult?.Select(t => t.Title);

        var viewModel = new RecipeVariantViewModel
        {
            VariantName = variantPage.Name,
            VariantDescription = variantPage.Description,
            Images = variantPage.Images,
            PrepTime = variantPage.PrepTime,
            CookTime = variantPage.CookTime,
            Servings = variantPage.ServingNumber,
            Tags = resolvedTags,
            Ingredients = JsonSerializer.DeserializeCollection<IngredientViewModel>(variantPage.Ingredients),
            Instructions = JsonSerializer.DeserializeCollection<InstructionViewModel>(variantPage.Instructions),
            VariantSlug = variantPage.GetUrl().RelativePath,
            RecipeName = recipePage?.Name,
            RecipeSlug = recipePage?.GetUrl().RelativePath,
            SiblingVariants = siblings.Select(s => new SiblingVariantViewModel
            {
                Name = s.Name,
                Slug = s.GetUrl().RelativePath,
            }),
            CreatedByName = await authorNameResolver.Resolve(variantPage.AuthorMemberGuid),
            ResourceStrings = GetStrings(),
        };

        await variantPage.MapMetadata(viewModel);
        return View("~/Features/Pages/RecipeVariantDetail/Index.cshtml", viewModel);
    }

    private Dictionary<string, string> GetStrings() => resourceStrings.GetManyOrDefault(
        "VariantDetail.CreatedBy",
        "VariantDetail.Ingredients"
    );
}
