using KCC.Contributions.Data;
using KCC.Web.Features.Models.Common;
using KCC.Web.Features.Providers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace KCC.Web.Features.Api;

[ApiController]
[Route("api/variant")]
[Authorize]
public class VariantCookedApiController(
    IVariantCookedInfoProvider cooked,
    IVariantGuidProvider variantGuidProvider,
    UserManager<KCCApplicationUser> userManager
) : ControllerBase
{
    [HttpPost("{variantGuid:guid}/cooked")]
    public async Task<IActionResult> MarkCooked(Guid variantGuid, CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return Unauthorized();
        }

        var recipeGuid = await variantGuidProvider.GetRecipeGuidAsync(variantGuid, cancellationToken);
        if (recipeGuid is null)
        {
            return NotFound(new { error = "Variant not found." });
        }

        cooked.MarkCooked(variantGuid, recipeGuid.Value, user.MemberGuid);
        return Ok(new { cookedCount = cooked.GetCookedCountForVariant(variantGuid), hasCooked = true });
    }

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
