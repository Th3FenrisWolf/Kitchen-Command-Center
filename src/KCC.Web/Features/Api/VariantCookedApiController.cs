using KCC.Contributions;
using KCC.Web.Features.Recipes;
using KCC.Web.Features.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Umbraco.Cms.Core.Security;

namespace KCC.Web.Features.Api;

[ApiController]
[Route("api/variant")]
[AutoValidateAntiforgeryToken]
[EnableRateLimiting(RateLimits.Contributions)]
public class VariantCookedApiController(
    IContributionWrites contributionWrites,
    IContributionStats contributionStats,
    IRecipeQueries recipes,
    IMemberManager memberManager) : ControllerBase
{
    [HttpPost("{variantGuid:guid}/cooked")]
    public async Task<IActionResult> MarkCooked(Guid variantGuid)
    {
        if (await memberManager.GetCurrentMemberAsync() is not { } member)
        {
            return Unauthorized();
        }

        if (!recipes.IsPublishedVariant(variantGuid))
        {
            return NotFound(new { error = "Variant not found." });
        }

        await contributionWrites.MarkCookedAsync(variantGuid, member.Key);
        return Ok(new CookedResponse((await contributionStats.GetAsync()).For(variantGuid).CookedCount, HasCooked: true));
    }

    [HttpDelete("{variantGuid:guid}/cooked")]
    public async Task<IActionResult> UnmarkCooked(Guid variantGuid)
    {
        if (await memberManager.GetCurrentMemberAsync() is not { } member)
        {
            return Unauthorized();
        }

        await contributionWrites.UnmarkCookedAsync(variantGuid, member.Key);
        return Ok(new CookedResponse((await contributionStats.GetAsync()).For(variantGuid).CookedCount, HasCooked: false));
    }
}
