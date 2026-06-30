using KCC.Contributions.Data;
using KCC.Web.Features.Models.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace KCC.Web.Features.Api;

/// <summary>Toggles and reports the current member's "I cooked this" mark on a variant.</summary>
[ApiController]
[Route("api/variant")]
[Authorize]
public class VariantCookedApiController(
    IVariantCookedInfoProvider cooked,
    IVariantGuidResolver variantGuidResolver,
    UserManager<KCCApplicationUser> userManager
) : ControllerBase
{
    /// <summary>Marks the current member as having cooked the variant (idempotent).</summary>
    /// <param name="variantGuid">The variant's content-item GUID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Ok with the updated cooked count; NotFound for an unknown variant; Unauthorized when not signed in.</returns>
    [HttpPost("{variantGuid:guid}/cooked")]
    public async Task<IActionResult> MarkCooked(Guid variantGuid, CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return Unauthorized();
        }

        var recipeGuid = await variantGuidResolver.ResolveRecipeGuidAsync(variantGuid, cancellationToken);
        if (recipeGuid is null)
        {
            return NotFound(new { error = "Variant not found." });
        }

        cooked.MarkCooked(variantGuid, recipeGuid.Value, user.MemberGuid);
        return Ok(new { cookedCount = cooked.GetCookedCountForVariant(variantGuid), hasCooked = true });
    }

    /// <summary>Removes the current member's cooked mark.</summary>
    /// <param name="variantGuid">The variant's content-item GUID.</param>
    /// <returns>Ok with the updated cooked count; Unauthorized when not signed in.</returns>
    [HttpDelete("{variantGuid:guid}/cooked")]
    public async Task<IActionResult> UnmarkCooked(Guid variantGuid)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return Unauthorized();
        }

        cooked.UnmarkCooked(variantGuid, user.MemberGuid);
        return Ok(new { cookedCount = cooked.GetCookedCountForVariant(variantGuid), hasCooked = false });
    }
}
