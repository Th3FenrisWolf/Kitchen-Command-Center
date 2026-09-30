namespace KCC.Web.Features.Api;

public sealed record ReviewsResponse(
    double Average,
    int Count,
    IReadOnlyList<int> Distribution,
    int Total,
    int Page,
    int PageSize,
    IReadOnlyList<ReviewItem> Reviews,
    MyReview MyReview);

public sealed record ReviewItem(string AuthorName, decimal Rating, string Text, DateTime Created, bool IsMine);

public sealed record MyReview(decimal Rating, string Text);

public sealed record CookNotesResponse(int Total, int Page, int PageSize, IReadOnlyList<CookNoteItem> Notes);

public sealed record CookNoteItem(int Id, string AuthorName, string Text, DateTime Created, bool IsMine);

public sealed record ReviewRequest(decimal Rating, string Text);

public sealed record CookedResponse(int CookedCount, bool HasCooked);
