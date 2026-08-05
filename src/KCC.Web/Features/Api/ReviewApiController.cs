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
public class ReviewApiController(
    IVariantReviewInfoProvider variantReviewProvider,
    IVariantGuidProvider variantGuidProvider,
    UserManager<KCCApplicationUser> userManager
) : ControllerBase
{
    public record ReviewRequest(decimal Rating, string Text);

    [HttpGet("{variantGuid:guid}/reviews")]
    [AllowAnonymous]
    public async Task<IActionResult> GetReviews(
        Guid variantGuid,
        [FromServices] AuthorNameProvider authorNameProvider,
        int page = 0,
        int pageSize = 10)
    {
        var aggregate = variantReviewProvider.GetAverageForVariant(variantGuid);
        var distribution = variantReviewProvider.GetDistributionForVariant(variantGuid);
        var memberGuid = await CurrentMemberGuidOrEmpty();

        var rows = variantReviewProvider.GetForVariant(variantGuid, page, pageSize, out var total);
        var authorNames = await authorNameProvider.ResolveMany(rows.Select(r => r.MemberGuid));

        var reviews = rows.Select(r => new
        {
            authorName = authorNames.GetValueOrDefault(r.MemberGuid) ?? "(deleted)",
            rating = r.Rating,
            text = r.ReviewText,
            created = r.ReviewCreated,
            isMine = memberGuid != Guid.Empty && r.MemberGuid == memberGuid,
        });

        var mine = memberGuid == Guid.Empty ? null : variantReviewProvider.GetMemberReview(variantGuid, memberGuid);

        return Ok(new
        {
            average = aggregate.Average,
            count = aggregate.Count,
            distribution,
            total,
            page,
            pageSize,
            reviews,
            myReview = mine is null ? null : new { rating = mine.Rating, text = mine.ReviewText },
        });
    }

    [HttpPut("{variantGuid:guid}/review")]
    public async Task<IActionResult> UpsertReview(
        Guid variantGuid,
        [FromBody] ReviewRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null || !VariantReviewInfoProvider.IsValidRating(request.Rating))
        {
            return BadRequest(new { error = "Rating must be between 0.5 and 5 in half-star steps." });
        }

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

        variantReviewProvider.Upsert(variantGuid, recipeGuid.Value, user.MemberGuid, request.Rating, request.Text);
        return Ok(new { success = true });
    }

    [HttpDelete("{variantGuid:guid}/review")]
    public async Task<IActionResult> DeleteReview(Guid variantGuid)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return Unauthorized();
        }

        var deleted = variantReviewProvider.DeleteOwn(variantGuid, user.MemberGuid);
        return deleted
            ? Ok(new { success = true })
            : NotFound(new { error = "No review to delete." });
    }

    private async Task<Guid> CurrentMemberGuidOrEmpty()
    {
        if (User?.Identity?.IsAuthenticated is not true)
        {
            return Guid.Empty;
        }

        var user = await userManager.GetUserAsync(User);
        return user?.MemberGuid ?? Guid.Empty;
    }
}
