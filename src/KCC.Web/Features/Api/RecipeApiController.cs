using KCC.Web.Features.Security;
using KCC.Web.Features.Submissions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Umbraco.Cms.Core.Security;

namespace KCC.Web.Features.Api;

[ApiController]
[Route("api/recipes")]
[AutoValidateAntiforgeryToken]
[EnableRateLimiting(RateLimits.Submissions)]
public class RecipeApiController(IRecipeSubmissions submissions, IMemberManager memberManager) : ControllerBase
{
    // Longer defeats Umbraco's ContentService.Save, which throws past 255 characters and would otherwise reach the
    // member as a 500.
    private const int MaxNameLength = 255;

    [HttpPost]
    public async Task<IActionResult> CreateRecipe([FromBody] CreateRecipeRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request?.RecipeName))
        {
            return BadRequest(new { error = "Recipe name is required." });
        }

        if (request.RecipeName.Trim().Length > MaxNameLength)
        {
            return BadRequest(new { error = "Recipe name cannot be more than 255 characters." });
        }

        if (string.IsNullOrWhiteSpace(request.FirstVariant?.VariantName))
        {
            return BadRequest(new { error = "First variant name is required." });
        }

        if (request.FirstVariant.VariantName.Trim().Length > MaxNameLength)
        {
            return BadRequest(new { error = "First variant name cannot be more than 255 characters." });
        }

        if (await memberManager.GetCurrentMemberAsync() is not { } member)
        {
            return Unauthorized();
        }

        return Ok(new { recipeKey = await submissions.SubmitRecipeAsync(request, member.Key, cancellationToken) });
    }

    [HttpPost("{recipeKey:guid}/variants")]
    public async Task<IActionResult> AddVariant(Guid recipeKey, [FromBody] CreateVariantRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request?.VariantName))
        {
            return BadRequest(new { error = "Variant name is required." });
        }

        if (request.VariantName.Trim().Length > MaxNameLength)
        {
            return BadRequest(new { error = "Variant name cannot be more than 255 characters." });
        }

        if (await memberManager.GetCurrentMemberAsync() is not { } member)
        {
            return Unauthorized();
        }

        return await submissions.SubmitVariantAsync(recipeKey, request, member.Key, cancellationToken) is { } variantKey
            ? Ok(new { variantKey })
            : NotFound(new { error = "Recipe not found." });
    }
}
