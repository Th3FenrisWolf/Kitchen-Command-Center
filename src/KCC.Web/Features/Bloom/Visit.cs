namespace KCC.Web.Features.Bloom;

public sealed record Visit(uint Seed, DateTimeOffset StartedAt, DateTimeOffset ResolvedAt)
{
    public static readonly TimeSpan MaxElapsed = TimeSpan.FromSeconds(300);

    public TimeSpan Elapsed => TimeSpan.FromTicks(Math.Clamp((ResolvedAt - StartedAt).Ticks, 0, MaxElapsed.Ticks));
}
