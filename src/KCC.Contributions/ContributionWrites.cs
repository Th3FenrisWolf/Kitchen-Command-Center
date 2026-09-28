using KCC.Contributions.Data;
using Microsoft.EntityFrameworkCore;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Persistence.EFCore.Scoping;

namespace KCC.Contributions;

public interface IContributionWrites
{
    Task UpsertReviewAsync(Guid variantKey, Guid memberKey, decimal rating, string text);

    Task<bool> DeleteReviewAsync(Guid variantKey, Guid memberKey);

    Task<int> AddNoteAsync(Guid variantKey, Guid memberKey, string text);

    Task<bool> DeleteOwnNoteAsync(int noteId, Guid memberKey);

    Task MarkCookedAsync(Guid variantKey, Guid memberKey);

    Task UnmarkCookedAsync(Guid variantKey, Guid memberKey);

    Task DeleteForVariantsAsync(IReadOnlyCollection<Guid> variantKeys);

    Task DeleteForMembersAsync(IReadOnlyCollection<Guid> memberKeys);
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

        await WriteAsync(async db =>
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
            return await db.SaveChangesAsync();
        });
        await ReviewsChangedAsync();
    }

    public async Task<bool> DeleteReviewAsync(Guid variantKey, Guid memberKey)
    {
        var deleted = await WriteAsync(db => db.Reviews
            .Where(review => review.VariantKey == variantKey && review.MemberKey == memberKey)
            .ExecuteDeleteAsync());
        if (deleted > 0)
        {
            await ReviewsChangedAsync();
        }

        return deleted > 0;
    }

    public Task<int> AddNoteAsync(Guid variantKey, Guid memberKey, string text)
    {
        var clamped = RatingMath.ClampText(text) ?? throw new ArgumentException("A cook note needs text.", nameof(text));
        return WriteAsync(async db =>
        {
            var now = DateTime.UtcNow;
            var note = new CookNote { VariantKey = variantKey, MemberKey = memberKey, Text = clamped, Created = now, Modified = now };
            db.CookNotes.Add(note);
            await db.SaveChangesAsync();
            return note.Id;
        });
    }

    public async Task<bool> DeleteOwnNoteAsync(int noteId, Guid memberKey) =>
        await WriteAsync(db => db.CookNotes.Where(note => note.Id == noteId && note.MemberKey == memberKey).ExecuteDeleteAsync()) > 0;

    public async Task MarkCookedAsync(Guid variantKey, Guid memberKey)
    {
        await WriteAsync(async db =>
        {
            if (await db.CookedMarks.AnyAsync(mark => mark.VariantKey == variantKey && mark.MemberKey == memberKey))
            {
                return 0;
            }

            db.CookedMarks.Add(new CookedMark { VariantKey = variantKey, MemberKey = memberKey, Created = DateTime.UtcNow });
            return await db.SaveChangesAsync();
        });
        stats.Invalidate();
    }

    public async Task UnmarkCookedAsync(Guid variantKey, Guid memberKey)
    {
        await WriteAsync(db => db.CookedMarks
            .Where(mark => mark.VariantKey == variantKey && mark.MemberKey == memberKey)
            .ExecuteDeleteAsync());
        stats.Invalidate();
    }

    public async Task DeleteForVariantsAsync(IReadOnlyCollection<Guid> variantKeys)
    {
        var reviews = await WriteAsync(async db =>
        {
            await db.CookNotes.Where(note => variantKeys.Contains(note.VariantKey)).ExecuteDeleteAsync();
            await db.CookedMarks.Where(mark => variantKeys.Contains(mark.VariantKey)).ExecuteDeleteAsync();
            return await db.Reviews.Where(review => variantKeys.Contains(review.VariantKey)).ExecuteDeleteAsync();
        });
        await DeletedAsync(reviews);
    }

    public async Task DeleteForMembersAsync(IReadOnlyCollection<Guid> memberKeys)
    {
        var reviews = await WriteAsync(async db =>
        {
            await db.CookNotes.Where(note => memberKeys.Contains(note.MemberKey)).ExecuteDeleteAsync();
            await db.CookedMarks.Where(mark => memberKeys.Contains(mark.MemberKey)).ExecuteDeleteAsync();
            return await db.Reviews.Where(review => memberKeys.Contains(review.MemberKey)).ExecuteDeleteAsync();
        });
        await DeletedAsync(reviews);
    }

    private async Task<T> WriteAsync<T>(Func<ContributionsDbContext, Task<T>> write)
    {
        using var scope = scopes.CreateScope();

        // Taken before the first read, so this transaction is already the writer when it looks at existing rows:
        // SQLite cannot upgrade a reader whose snapshot another writer has moved past.
        scope.WriteLock(ContributionLocks.Contributions);
        var result = await scope.ExecuteWithContextAsync(write);
        scope.Complete();
        return result;
    }

    private async Task ReviewsChangedAsync()
    {
        stats.Invalidate();
        await eventAggregator.PublishAsync(new ReviewsChangedNotification());
    }

    private async Task DeletedAsync(int reviews)
    {
        if (reviews > 0)
        {
            await ReviewsChangedAsync();
        }
        else
        {
            stats.Invalidate();
        }
    }
}
