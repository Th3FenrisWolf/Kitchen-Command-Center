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
        if ((RefuseName(request?.RecipeName, "Recipe name", "recipeName")
            ?? RefuseName(request?.FirstVariant?.VariantName, "First variant name", "variantName")) is { } refusal)
        {
            return BadRequest(refusal);
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
        if (RefuseName(request?.VariantName, "Variant name", "variantName") is { } refusal)
        {
            return BadRequest(refusal);
        }

        if (await memberManager.GetCurrentMemberAsync() is not { } member)
        {
            return Unauthorized();
        }

        return await submissions.SubmitVariantAsync(recipeKey, request, member.Key, cancellationToken) is { } variantKey
            ? Ok(new { variantKey })
            : NotFound(new { error = "Recipe not found." });
    }

    private static NameRefusal RefuseName(string name, string label, string field) =>
        string.IsNullOrWhiteSpace(name) ? new NameRefusal($"{label} is required.", null, null)
        : name.Trim().Length > MaxNameLength ? new NameRefusal($"{label} cannot be more than {MaxNameLength} characters.", NameRefusal.TooLong, field)
        : null;
}

public sealed record NameRefusal(string Error, string Code, string Field)
{
    public const string TooLong = "nameTooLong";
}
