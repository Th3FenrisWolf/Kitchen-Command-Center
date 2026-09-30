using KCC.Contributions;
using KCC.Web.Features.Providers;
using KCC.Web.Features.Recipes;
using KCC.Web.Features.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Umbraco.Cms.Core.Security;

namespace KCC.Web.Features.Api;

[ApiController]
[Route("api/variant")]
[AutoValidateAntiforgeryToken]
public class ReviewApiController(
    IContributionStats contributionStats,
    IContributionReads contributionReads,
    IContributionWrites contributionWrites,
    IRecipeQueries recipes,
    IAuthorNameProvider authorNames,
    IMemberManager memberManager) : ControllerBase
{
    [HttpGet("{variantGuid:guid}/reviews")]
    public async Task<ActionResult<ReviewsResponse>> GetReviews(Guid variantGuid, int page = 0, int pageSize = 10)
    {
        var stats = (await contributionStats.GetAsync()).For(variantGuid);
        var memberKey = (await memberManager.GetCurrentMemberAsync())?.Key;
        var reviews = await contributionReads.ReviewsAsync(variantGuid, page, pageSize);
        var names = await authorNames.ResolveMany(reviews.Items.Select(review => review.MemberKey));
        var mine = memberKey is { } key ? await contributionReads.MemberReviewAsync(variantGuid, key) : null;

        return new ReviewsResponse(
            stats.Rating.Average,
            stats.Rating.Count,
            stats.Distribution,
            reviews.Total,
            Math.Max(0, page),
            Math.Clamp(pageSize, 1, ContributionReads.MaxPageSize),
            reviews.Items.Select(review => new ReviewItem(
                names.GetValueOrDefault(review.MemberKey) ?? AuthorNameProvider.DeletedMemberName,
                review.Rating,
                review.Text,
                review.Created,
                review.MemberKey == memberKey)).ToList(),
            mine is null ? null : new MyReview(mine.Rating, mine.Text));
    }

    [HttpPut("{variantGuid:guid}/review")]
    [EnableRateLimiting(RateLimits.Contributions)]
    public async Task<IActionResult> UpsertReview(Guid variantGuid, [FromBody] ReviewRequest request)
    {
        if (request is null || !RatingMath.IsValidRating(request.Rating))
        {
            return BadRequest(new { error = "Rating must be between 0.5 and 5 in half-star steps." });
        }

        if (await memberManager.GetCurrentMemberAsync() is not { } member)
        {
            return Unauthorized();
        }

        if (!recipes.IsPublishedVariant(variantGuid))
        {
            return NotFound(new { error = "Variant not found." });
        }

        await contributionWrites.UpsertReviewAsync(variantGuid, member.Key, request.Rating, request.Text);
        return Ok(new { success = true });
    }

    [HttpDelete("{variantGuid:guid}/review")]
    [EnableRateLimiting(RateLimits.Contributions)]
    public async Task<IActionResult> DeleteReview(Guid variantGuid)
    {
        if (await memberManager.GetCurrentMemberAsync() is not { } member)
        {
            return Unauthorized();
        }

        return await contributionWrites.DeleteReviewAsync(variantGuid, member.Key)
            ? Ok(new { success = true })
            : NotFound(new { error = "No review to delete." });
    }
}
