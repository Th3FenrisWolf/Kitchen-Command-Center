using KCC.Contributions;
using KCC.Web.Features.Providers;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core.Security;

namespace KCC.Web.Features.Api;

[ApiController]
[Route("api/variant")]
public class ReviewApiController(
    IContributionStats contributionStats,
    IContributionReads contributionReads,
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
}
