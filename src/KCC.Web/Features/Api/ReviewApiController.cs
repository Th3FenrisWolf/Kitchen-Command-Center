using KCC.Contributions.Data;
using KCC.Web.Features.Members;
using KCC.Web.Features.Models.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace KCC.Web.Features.Api;

/// <summary>Read/write API for per-variant ratings and reviews.</summary>
[ApiController]
[Route("api/variant")]
[Authorize]
public class ReviewApiController(
    IVariantReviewInfoProvider reviews,
    IVariantGuidResolver variantGuidResolver,
    UserManager<KCCApplicationUser> userManager
) : ControllerBase
{
    /// <summary>Upsert request body.</summary>
    /// <param name="Rating">The 0.5-5 star rating, in half-star steps.</param>
    /// <param name="Text">The optional review text.</param>
    public record ReviewRequest(decimal Rating, string Text);

    /// <summary>Returns the variant's average + count, the current member's review, and a page of reviews.</summary>
    /// <param name="variantGuid">The variant's content-item GUID.</param>
    /// <param name="authorNameResolver">Resolves member display names for attribution.</param>
    /// <param name="page">Zero-based page index.</param>
    /// <param name="pageSize">Page size.</param>
    /// <returns>The aggregate, the current member's review, and a page of reviews.</returns>
    [HttpGet("{variantGuid:guid}/reviews")]
    [AllowAnonymous]
    public async Task<IActionResult> GetReviews(
        Guid variantGuid,
        [FromServices] IAuthorNameResolver authorNameResolver,
        int page = 0,
        int pageSize = 10)
    {
        var aggregate = reviews.GetAverageForVariant(variantGuid);
        var memberGuid = await CurrentMemberGuidOrEmpty();

        var rows = reviews.GetForVariant(variantGuid, page, pageSize, out var total);
        var authorNames = await authorNameResolver.ResolveMany(rows.Select(r => r.MemberGuid));

        var dtos = rows.Select(r => new
        {
            authorName = authorNames.GetValueOrDefault(r.MemberGuid) ?? "(deleted)",
            rating = r.Rating,
            text = r.ReviewText,
            created = r.ReviewCreated,
            isMine = memberGuid != Guid.Empty && r.MemberGuid == memberGuid,
        });

        var mine = memberGuid == Guid.Empty ? null : reviews.GetMemberReview(variantGuid, memberGuid);

        return Ok(new
        {
            average = aggregate.Average,
            count = aggregate.Count,
            total,
            page,
            pageSize,
            reviews = dtos,
            myReview = mine is null ? null : new { rating = mine.Rating, text = mine.ReviewText },
        });
    }

    /// <summary>Creates or updates the current member's review for the variant.</summary>
    /// <param name="variantGuid">The variant's content-item GUID.</param>
    /// <param name="request">The rating + text to store.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Ok on success; BadRequest for an invalid rating; NotFound for an unknown variant; Unauthorized when not signed in.</returns>
    [HttpPut("{variantGuid:guid}/review")]
    public async Task<IActionResult> UpsertReview(Guid variantGuid, [FromBody] ReviewRequest request, CancellationToken cancellationToken)
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

        var recipeGuid = await variantGuidResolver.ResolveRecipeGuidAsync(variantGuid, cancellationToken);
        if (recipeGuid is null)
        {
            return NotFound(new { error = "Variant not found." });
        }

        reviews.Upsert(variantGuid, recipeGuid.Value, user.MemberGuid, request.Rating, request.Text);
        return Ok(new { success = true });
    }

    /// <summary>Deletes the current member's review for the variant.</summary>
    /// <param name="variantGuid">The variant's content-item GUID.</param>
    /// <returns>Ok when a review was removed; NotFound when there was none; Unauthorized when not signed in.</returns>
    [HttpDelete("{variantGuid:guid}/review")]
    public async Task<IActionResult> DeleteReview(Guid variantGuid)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return Unauthorized();
        }

        var deleted = reviews.DeleteOwn(variantGuid, user.MemberGuid);
        return deleted ? Ok(new { success = true }) : NotFound(new { error = "No review to delete." });
    }

    private async Task<Guid> CurrentMemberGuidOrEmpty()
    {
        if (User?.Identity?.IsAuthenticated != true)
        {
            return Guid.Empty;
        }

        var user = await userManager.GetUserAsync(User);
        return user?.MemberGuid ?? Guid.Empty;
    }
}
