namespace KCC.Contributions.Dashboard;

public sealed record WaitingModel(IReadOnlyList<WaitingMember> Members, IReadOnlyList<WaitingDraft> Drafts);

public sealed record WaitingMember(Guid Key, string Name, string UserName, string Email, DateTime Registered);

public sealed record WaitingDraft(Guid Key, string Kind, string Name, string RecipeName, string AuthorName, DateTime Created);

public sealed record EntryPage(int Total, int Page, int PageSize, IReadOnlyList<Entry> Items);

public sealed record Entry(int Id, Guid VariantKey, string VariantName, string RecipeName, string MemberName, decimal? Rating, string Text, DateTime Created);

public sealed record ReviewEdit(decimal Rating, string Text);

public sealed record NoteEdit(string Text);
