using KCC.Contributions.Data;
using Microsoft.EntityFrameworkCore;
using Umbraco.Cms.Persistence.EFCore.Scoping;

namespace KCC.Contributions;

public sealed record Paged<T>(IReadOnlyList<T> Items, int Total);

public interface IContributionReads
{
    Task<Paged<Review>> ReviewsAsync(Guid variantKey, int page, int pageSize);

    Task<Review> MemberReviewAsync(Guid variantKey, Guid memberKey);

    Task<Paged<CookNote>> NotesAsync(Guid variantKey, int page, int pageSize);

    Task<bool> HasCookedAsync(Guid variantKey, Guid memberKey);

    Task<Paged<Review>> LatestReviewsAsync(int page, int pageSize);

    Task<Paged<CookNote>> LatestNotesAsync(int page, int pageSize);
}

public sealed class ContributionReads(IEFCoreScopeProvider<ContributionsDbContext> scopes) : IContributionReads
{
    public const int MaxPageSize = 50;

    public Task<Paged<Review>> ReviewsAsync(Guid variantKey, int page, int pageSize) =>
        ReadAsync(db => PageAsync(
            db.Reviews.AsNoTracking()
                .Where(review => review.VariantKey == variantKey)
                .OrderByDescending(review => review.Created)
                .ThenByDescending(review => review.Id),
            page,
            pageSize));

    public Task<Review> MemberReviewAsync(Guid variantKey, Guid memberKey) =>
        ReadAsync(db => db.Reviews.AsNoTracking()
            .FirstOrDefaultAsync(review => review.VariantKey == variantKey && review.MemberKey == memberKey));

    public Task<Paged<CookNote>> NotesAsync(Guid variantKey, int page, int pageSize) =>
        ReadAsync(db => PageAsync(
            db.CookNotes.AsNoTracking()
                .Where(note => note.VariantKey == variantKey)
                .OrderByDescending(note => note.Created)
                .ThenByDescending(note => note.Id),
            page,
            pageSize));

    public Task<bool> HasCookedAsync(Guid variantKey, Guid memberKey) =>
        ReadAsync(db => db.CookedMarks.AnyAsync(mark => mark.VariantKey == variantKey && mark.MemberKey == memberKey));

    public Task<Paged<Review>> LatestReviewsAsync(int page, int pageSize) =>
        ReadAsync(db => PageAsync(
            db.Reviews.AsNoTracking().OrderByDescending(review => review.Created).ThenByDescending(review => review.Id),
            page,
            pageSize));

    public Task<Paged<CookNote>> LatestNotesAsync(int page, int pageSize) =>
        ReadAsync(db => PageAsync(
            db.CookNotes.AsNoTracking().OrderByDescending(note => note.Created).ThenByDescending(note => note.Id),
            page,
            pageSize));

    private static async Task<Paged<T>> PageAsync<T>(IQueryable<T> query, int page, int pageSize)
    {
        var size = Math.Clamp(pageSize, 1, MaxPageSize);
        var total = await query.CountAsync();
        var items = await query.Skip(Math.Max(0, page) * size).Take(size).ToListAsync();
        return new Paged<T>(items, total);
    }

    private async Task<T> ReadAsync<T>(Func<ContributionsDbContext, Task<T>> read)
    {
        using var scope = scopes.CreateScope();
        var result = await scope.ExecuteWithContextAsync(read);
        scope.Complete();
        return result;
    }
}
