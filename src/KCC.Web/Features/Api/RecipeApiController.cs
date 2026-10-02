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
public class RecipeApiController(RecipeSubmissions submissions, IMemberManager memberManager) : ControllerBase
{
    // Longer defeats Umbraco's ContentService.Save, which throws past 255 characters and would otherwise reach the
    // member as a 500.
    private const int MaxNameLength = 255;

    [HttpPost]
    public async Task<IActionResult> CreateRecipe([FromBody] CreateRecipeRequest request, CancellationToken cancellationToken)
    {
        if ((NameError(request?.RecipeName, "Recipe name") ?? NameError(request?.FirstVariant?.VariantName, "First variant name")) is { } error)
        {
            return BadRequest(new { error });
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
        if (NameError(request?.VariantName, "Variant name") is { } error)
        {
            return BadRequest(new { error });
        }

        if (await memberManager.GetCurrentMemberAsync() is not { } member)
        {
            return Unauthorized();
        }

        return await submissions.SubmitVariantAsync(recipeKey, request, member.Key, cancellationToken) is { } variantKey
            ? Ok(new { variantKey })
            : NotFound(new { error = "Recipe not found." });
    }

    private static string NameError(string name, string label) =>
        string.IsNullOrWhiteSpace(name) ? $"{label} is required."
        : name.Trim().Length > MaxNameLength ? $"{label} cannot be more than {MaxNameLength} characters."
        : null;
}
