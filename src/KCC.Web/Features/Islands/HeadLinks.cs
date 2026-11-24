namespace KCC.Web.Features.Islands;

public sealed record HeadLinks(IReadOnlyList<string> Stylesheets, IReadOnlyList<string> ModulePreloads)
{
    public static HeadLinks None { get; } = new([], []);
}
