namespace KCC.Web.Features.Bloom;

public sealed record BloomInputs(uint Seed, string Page)
{
    public static BloomInputs For(HttpContext context, Visit visit) => new(visit.Seed, PageKey(context.Request.Path));

    public static string PageKey(PathString path)
    {
        var page = (path.Value ?? string.Empty).TrimEnd('/').ToLowerInvariant();
        return page.Length == 0 ? "/" : page;
    }
}
