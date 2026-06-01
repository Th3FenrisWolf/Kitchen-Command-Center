using System.Text.Json;
using CMS.ContentEngine;
using CMS.Membership;
using CMS.Websites;
using CMS.Websites.Routing;
using KCC.Admin;
using Kentico.Content.Web.Mvc.Routing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KCC.Web.Features.Api;

/// <summary>
/// API controller for creating recipes and recipe variants from the live site form.
/// Pages are created in draft/unpublished state.
/// </summary>
[ApiController]
[Route("api/recipes")]
[Authorize]
public class RecipeApiController(
    IWebPageManagerFactory webPageManagerFactory,
    IWebsiteChannelContext websiteChannelContext,
    IPreferredLanguageRetriever preferredLanguageRetriever,
    IUserInfoProvider userInfoProvider,
    IRecipeIconService recipeIconService
) : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>
    /// Creates a new recipe page and its first variant as a child page, both in draft state.
    /// </summary>
    /// <param name="request">The create recipe request payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An <see cref="IActionResult"/> containing the new recipe's web page item ID.</returns>
    [HttpPost]
    public async Task<IActionResult> CreateRecipe(
        [FromBody] CreateRecipeRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RecipeName))
        {
            return BadRequest(new { error = "Recipe name is required." });
        }

        if (string.IsNullOrWhiteSpace(request.FirstVariant?.VariantName))
        {
            return BadRequest(new { error = "First variant name is required." });
        }

        string languageName = preferredLanguageRetriever.Get();
        var webPageManager = CreateManager();

        string icon = await recipeIconService.PickAsync(
            request.RecipeName,
            request.RecipeDescription,
            request.FirstVariant.Ingredients.Select(i => i.Name),
            cancellationToken);

        var recipeData = new ContentItemData(new Dictionary<string, object>
        {
            [nameof(KCC.Recipe.Name)] = request.RecipeName,
            [nameof(KCC.Recipe.Description)] = request.RecipeDescription ?? string.Empty,
            [nameof(KCC.Recipe.Icon)] = icon,
        });

        var recipeContentItemParams = new ContentItemParameters(KCC.Recipe.CONTENT_TYPE_NAME, recipeData);

        var recipePageParams = new CreateWebPageParameters(
            request.RecipeName,
            languageName,
            recipeContentItemParams);

        var recipeId = await webPageManager.Create(recipePageParams, cancellationToken);

        var variantData = new ContentItemData(new Dictionary<string, object>
        {
            [nameof(KCC.RecipeVariant.Name)] = request.FirstVariant.VariantName,
            [nameof(KCC.RecipeVariant.Description)] = request.FirstVariant.VariantDescription ?? string.Empty,
            [nameof(KCC.RecipeVariant.PrepTime)] = request.FirstVariant.PrepTime ?? 0,
            [nameof(KCC.RecipeVariant.CookTime)] = request.FirstVariant.CookTime ?? 0,
            [nameof(KCC.RecipeVariant.ServingNumber)] = request.FirstVariant.Servings ?? 0,
            [nameof(KCC.RecipeVariant.Ingredients)] = JsonSerializer.Serialize(request.FirstVariant.Ingredients, JsonOptions),
            [nameof(KCC.RecipeVariant.Instructions)] = JsonSerializer.Serialize(request.FirstVariant.Instructions, JsonOptions),
        });

        var variantContentItemParams = new ContentItemParameters(KCC.RecipeVariant.CONTENT_TYPE_NAME, variantData);

        var variantPageParams = new CreateWebPageParameters(
            request.FirstVariant.VariantName,
            languageName,
            variantContentItemParams)
        {
            ParentWebPageItemID = recipeId,
        };

        await webPageManager.Create(variantPageParams, cancellationToken);

        return Ok(new { recipeId });
    }

    /// <summary>
    /// Adds a new variant as a child page of an existing recipe, in draft state.
    /// </summary>
    /// <param name="recipeWebPageId">The web page item ID of the parent recipe.</param>
    /// <param name="request">The create variant request payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An <see cref="IActionResult"/> containing the new variant's web page item ID.</returns>
    [HttpPost("{recipeWebPageId:int}/variants")]
    public async Task<IActionResult> AddVariant(
        int recipeWebPageId,
        [FromBody] CreateVariantRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.VariantName))
        {
            return BadRequest(new { error = "Variant name is required." });
        }

        string languageName = preferredLanguageRetriever.Get();
        var webPageManager = CreateManager();

        var variantData = new ContentItemData(new Dictionary<string, object>
        {
            [nameof(RecipeVariant.Name)] = request.VariantName,
            [nameof(RecipeVariant.Description)] = request.VariantDescription ?? string.Empty,
            [nameof(RecipeVariant.PrepTime)] = request.PrepTime ?? 0,
            [nameof(RecipeVariant.CookTime)] = request.CookTime ?? 0,
            [nameof(RecipeVariant.ServingNumber)] = request.Servings ?? 0,
            [nameof(RecipeVariant.Ingredients)] = JsonSerializer.Serialize(request.Ingredients, JsonOptions),
            [nameof(RecipeVariant.Instructions)] = JsonSerializer.Serialize(request.Instructions, JsonOptions),
        });

        var variantContentItemParams = new ContentItemParameters(RecipeVariant.CONTENT_TYPE_NAME, variantData);

        var variantPageParams = new CreateWebPageParameters(
            request.VariantName,
            languageName,
            variantContentItemParams)
        {
            ParentWebPageItemID = recipeWebPageId,
        };

        var variantId = await webPageManager.Create(variantPageParams, cancellationToken);

        return Ok(new { variantId });
    }

    private IWebPageManager CreateManager()
    {
        var user = userInfoProvider.Get()
            .WhereEquals(nameof(UserInfo.UserName), "administrator")
            .FirstOrDefault() ?? new UserInfo();

        return webPageManagerFactory.Create(websiteChannelContext.WebsiteChannelID, user.UserID);
    }
}
