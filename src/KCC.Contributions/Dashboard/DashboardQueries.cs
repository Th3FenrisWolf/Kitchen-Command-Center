using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.Navigation;

namespace KCC.Contributions.Dashboard;

public sealed class DashboardQueries(
    IContributionReads contributionReads,
    IDashboardMembers members,
    IMemberService memberService,
    IContentService contentService,
    IEntityService entityService,
    IDocumentNavigationQueryService navigation,
    IPublishStatusQueryService publishStatus)
{
    public const string RecipeKind = "recipe";

    public const string VariantKind = "variant";

    private const int MemberBatch = 100;

    public async Task<WaitingModel> WaitingAsync() => new(await UnapprovedMembersAsync(), await DraftsAsync());

    public async Task<EntryPage> ReviewsAsync(int page, int pageSize) =>
        await PageAsync(
            await contributionReads.LatestReviewsAsync(page, pageSize),
            page,
            pageSize,
            review => new Row(review.Id, review.VariantKey, review.MemberKey, review.Rating, review.Text, review.Created));

    public async Task<EntryPage> NotesAsync(int page, int pageSize) =>
        await PageAsync(
            await contributionReads.LatestNotesAsync(page, pageSize),
            page,
            pageSize,
            note => new Row(note.Id, note.VariantKey, note.MemberKey, null, note.Text, note.Created));

    // Umbraco stores its dates in UTC, but SQLite hands them back unmarked.
    private static DateTime Utc(DateTime value) =>
        value.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(value, DateTimeKind.Utc) : value.ToUniversalTime();

    // A member picker saves its value as a member UDI, such as umb://member/0a1b…
    private static Guid? AuthorKey(IContent content) =>
        content.GetValue("author") is string text && UdiParser.TryParse(text, out Udi udi) && udi is GuidUdi member ? member.Guid : null;

    private async Task<IReadOnlyList<WaitingMember>> UnapprovedMembersAsync()
    {
        // Umbraco's member filter cannot order by date, so the whole list is read before it is sorted.
        var unapproved = new List<IMember>();
        var filter = new MemberFilter { IsApproved = false };
        while (true)
        {
            var batch = await memberService.FilterAsync(filter, skip: unapproved.Count, take: MemberBatch);
            unapproved.AddRange(batch.Items);
            if (!batch.Items.Any() || unapproved.Count >= batch.Total)
            {
                break;
            }
        }

        return unapproved
            .OrderByDescending(member => member.CreateDate)
            .Select(member => new WaitingMember(member.Key, member.Name, member.Username, member.Email, Utc(member.CreateDate)))
            .ToList();
    }

    private async Task<IReadOnlyList<WaitingDraft>> DraftsAsync()
    {
        var recipeKeys = RecipeKeys();
        var recipeOfVariant = new Dictionary<Guid, Guid>();
        foreach (var recipeKey in recipeKeys)
        {
            if (navigation.TryGetChildrenKeysOfType(recipeKey, "recipeVariant", out var variantKeys))
            {
                foreach (var variantKey in variantKeys)
                {
                    recipeOfVariant[variantKey] = recipeKey;
                }
            }
        }

        var unpublished = recipeKeys.Concat(recipeOfVariant.Keys)
            .Where(key => !publishStatus.IsDocumentPublishedInAnyCulture(key))
            .ToList();
        if (unpublished.Count == 0)
        {
            return [];
        }

        var drafts = contentService.GetByIds(unpublished).ToList();
        var recipeNames = Names(recipeOfVariant.Where(pair => unpublished.Contains(pair.Key)).Select(pair => pair.Value));
        var authors = await members.NamesAsync(drafts.Select(AuthorKey).OfType<Guid>());

        return drafts
            .OrderByDescending(draft => draft.CreateDate)
            .Select(draft =>
            {
                var isVariant = recipeOfVariant.TryGetValue(draft.Key, out var recipeKey);
                return new WaitingDraft(
                    draft.Key,
                    isVariant ? VariantKind : RecipeKind,
                    draft.Name,
                    isVariant ? recipeNames.GetValueOrDefault(recipeKey) : null,
                    AuthorKey(draft) is { } author ? authors.GetValueOrDefault(author) : null,
                    Utc(draft.CreateDate));
            })
            .ToList();
    }

    private List<Guid> RecipeKeys() =>
        navigation.TryGetRootKeys(out var roots)
            ? roots.SelectMany(root => navigation.TryGetDescendantsKeysOfType(root, "recipe", out var keys) ? keys : []).ToList()
            : [];

    private Dictionary<Guid, string> Names(IEnumerable<Guid> keys)
    {
        var distinct = keys.Distinct().ToArray();
        return distinct.Length == 0
            ? []
            : entityService.GetAll(UmbracoObjectTypes.Document, distinct).ToDictionary(entity => entity.Key, entity => entity.Name);
    }

    private async Task<EntryPage> PageAsync<T>(Paged<T> paged, int page, int pageSize, Func<T, Row> toRow)
    {
        var rows = paged.Items.Select(toRow).ToList();
        var variants = rows.Count == 0
            ? []
            : entityService.GetAll(UmbracoObjectTypes.Document, rows.Select(row => row.VariantKey).Distinct().ToArray()).ToDictionary(entity => entity.Key);
        var recipeIds = variants.Values.Where(variant => !variant.Trashed).Select(variant => variant.ParentId).Distinct().ToArray();
        var recipes = recipeIds.Length == 0
            ? []
            : entityService.GetAll(UmbracoObjectTypes.Document, recipeIds).ToDictionary(entity => entity.Id, entity => entity.Name);
        var names = await members.NamesAsync(rows.Select(row => row.MemberKey));

        return new EntryPage(
            paged.Total,
            Math.Max(0, page),
            Math.Clamp(pageSize, 1, ContributionReads.MaxPageSize),
            rows.Select(row =>
            {
                var variant = variants.GetValueOrDefault(row.VariantKey);
                return new Entry(
                    row.Id,
                    row.VariantKey,
                    variant?.Name,
                    variant is { Trashed: false } ? recipes.GetValueOrDefault(variant.ParentId) : null,
                    names.GetValueOrDefault(row.MemberKey),
                    row.Rating,
                    row.Text,
                    row.Created);
            }).ToList());
    }

    private sealed record Row(int Id, Guid VariantKey, Guid MemberKey, decimal? Rating, string Text, DateTime Created);
}
