using KCC.Contributions.Data;
using Microsoft.EntityFrameworkCore;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Persistence.EFCore.Scoping;

namespace KCC.Contributions;

public interface IContributionWrites
{
    Task UpsertReviewAsync(Guid variantKey, Guid memberKey, decimal rating, string text);
}

public sealed class ContributionWrites(
    IEFCoreScopeProvider<ContributionsDbContext> scopes,
    IContributionStats stats,
    IEventAggregator eventAggregator) : IContributionWrites
{
    public async Task UpsertReviewAsync(Guid variantKey, Guid memberKey, decimal rating, string text)
    {
        if (!RatingMath.IsValidRating(rating))
        {
            throw new ArgumentOutOfRangeException(nameof(rating), rating, "A rating runs from 0.5 to 5 in half-star steps.");
        }

        using (var scope = scopes.CreateScope())
        {
            // Taken before the read below so this transaction is already the writer when it looks for the
            // member's review: SQLite cannot upgrade a reader whose snapshot another writer has moved past.
            scope.WriteLock(ContributionLocks.Contributions);
            await scope.ExecuteWithContextAsync<Task>(async db =>
            {
                var now = DateTime.UtcNow;
                var review = await db.Reviews.FirstOrDefaultAsync(r => r.VariantKey == variantKey && r.MemberKey == memberKey);
                if (review is null)
                {
                    review = new Review { VariantKey = variantKey, MemberKey = memberKey, Created = now };
                    db.Reviews.Add(review);
                }

                review.Rating = rating;
                review.Text = RatingMath.ClampText(text);
                review.Modified = now;
                await db.SaveChangesAsync();
            });
            scope.Complete();
        }

        stats.Invalidate();
        await eventAggregator.PublishAsync(new ReviewsChangedNotification());
    }
}
