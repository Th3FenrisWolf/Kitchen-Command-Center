using System.Globalization;

namespace KCC.E2ETests.Config;

public sealed class BootDeadline
{
    private static readonly TimeSpan SleepThreshold = TimeSpan.FromSeconds(30);

    private readonly TimeProvider clock;
    private readonly TimeSpan limit;
    private readonly DateTimeOffset wallStart;
    private readonly long awakeStart;

    public BootDeadline(TimeSpan limit, TimeProvider? clock = null)
    {
        this.limit = limit;
        this.clock = clock ?? TimeProvider.System;
        wallStart = this.clock.GetUtcNow();
        awakeStart = this.clock.GetTimestamp();
    }

    public bool Expired => clock.GetUtcNow() - wallStart >= limit;

    // The wall clock jumps across a system sleep and the awake clock does not, so their gap is the time slept.
    public string Describe(string timeout)
    {
        var slept = (clock.GetUtcNow() - wallStart) - clock.GetElapsedTime(awakeStart);
        if (slept <= SleepThreshold)
        {
            return timeout;
        }

        var whole = TimeSpan.FromSeconds(Math.Round(slept.TotalSeconds));
        var gap = string.Create(CultureInfo.InvariantCulture, $"{(int)whole.TotalMinutes} min {whole.Seconds} s");
        return $"{timeout} The machine slept {gap} during boot, so this is not a database fault.";
    }
}
