using System.Text.Json;
using CMS.ContentEngine;
using CMS.Membership;
using CMS.Websites;
using CMS.Websites.Routing;
using KCC.Admin;
using KCC.Web.Features.Models.Common;
using Kentico.Content.Web.Mvc.Routing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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
    IRecipeIconService recipeIconService,
    UserManager<KCCApplicationUser> userManager
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

        var author = await userManager.GetUserAsync(User);
        if (author is null)
        {
            return Unauthorized();
        }

        string languageName = preferredLanguageRetriever.Get();
        var webPageManager = CreateManager();

        string icon = await recipeIconService.PickAsync(
            request.RecipeName,
            request.RecipeDescription,
            request.FirstVariant.Ingredients.Select(i => i.Name),
            cancellationToken);

        var recipeData = new ContentItemData(BuildRecipeData(request, icon, author.MemberGuid));

        var recipeContentItemParams = new ContentItemParameters(KCC.Recipe.CONTENT_TYPE_NAME, recipeData);

        var recipePageParams = new CreateWebPageParameters(
            request.RecipeName,
            languageName,
            recipeContentItemParams);

        var recipeId = await webPageManager.Create(recipePageParams, cancellationToken);

        string variantIcon = await recipeIconService.PickAsync(
            request.FirstVariant.VariantName,
            request.FirstVariant.VariantDescription,
            request.FirstVariant.Ingredients.Select(i => i.Name),
            cancellationToken);

        var variantData = new ContentItemData(BuildVariantData(request.FirstVariant, variantIcon, author.MemberGuid));

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

        var author = await userManager.GetUserAsync(User);
        if (author is null)
        {
            return Unauthorized();
        }

        string languageName = preferredLanguageRetriever.Get();
        var webPageManager = CreateManager();

        string icon = await recipeIconService.PickAsync(
            request.VariantName,
            request.VariantDescription,
            request.Ingredients.Select(i => i.Name),
            cancellationToken);

        var variantData = new ContentItemData(BuildVariantData(request, icon, author.MemberGuid));

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

    /// <summary>
    /// Builds the content item data for a new recipe page, stamping the authoring member.
    /// </summary>
    /// <param name="request">The create recipe request payload.</param>
    /// <param name="icon">The resolved Font Awesome icon class for the recipe.</param>
    /// <param name="authorMemberGuid">The GUID of the member creating the recipe.</param>
    /// <returns>A dictionary of field name/value pairs for use in <see cref="ContentItemData"/>.</returns>
    public static Dictionary<string, object> BuildRecipeData(CreateRecipeRequest request, string icon, Guid authorMemberGuid) => new()
    {
        [nameof(Recipe.Name)] = request.RecipeName,
        [nameof(Recipe.Description)] = request.RecipeDescription ?? string.Empty,
        [nameof(Recipe.Icon)] = icon,
        [nameof(Recipe.AuthorMemberGuid)] = authorMemberGuid,
    };

    /// <summary>
    /// Builds the content item data for a new recipe variant page, stamping the authoring member.
    /// </summary>
    /// <param name="request">The create variant request payload.</param>
    /// <param name="icon">The resolved Font Awesome icon class for the variant.</param>
    /// <param name="authorMemberGuid">The GUID of the member creating the variant.</param>
    /// <returns>A dictionary of field name/value pairs for use in <see cref="ContentItemData"/>.</returns>
    public static Dictionary<string, object> BuildVariantData(CreateVariantRequest request, string icon, Guid authorMemberGuid) => new()
    {
        [nameof(RecipeVariant.Name)] = request.VariantName,
        [nameof(RecipeVariant.Description)] = request.VariantDescription ?? string.Empty,
        [nameof(RecipeVariant.Icon)] = icon,
        [nameof(RecipeVariant.PrepTime)] = request.PrepTime ?? 0,
        [nameof(RecipeVariant.CookTime)] = request.CookTime ?? 0,
        [nameof(RecipeVariant.ServingNumber)] = request.Servings ?? 0,
        [nameof(RecipeVariant.Ingredients)] = JsonSerializer.Serialize(request.Ingredients, JsonOptions),
        [nameof(RecipeVariant.Instructions)] = JsonSerializer.Serialize(request.Instructions, JsonOptions),
        [nameof(RecipeVariant.AuthorMemberGuid)] = authorMemberGuid,
    };

    private IWebPageManager CreateManager()
    {
        var user = userInfoProvider.Get()
            .WhereEquals(nameof(UserInfo.UserName), "administrator")
            .FirstOrDefault() ?? new UserInfo();

        return webPageManagerFactory.Create(websiteChannelContext.WebsiteChannelID, user.UserID);
    }
}
