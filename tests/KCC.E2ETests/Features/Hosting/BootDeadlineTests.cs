using KCC.E2ETests.Config;

namespace KCC.E2ETests.Features.Hosting;

public class BootDeadlineTests
{
    [Test]
    public async Task Expired_AfterTheMachineSlept_NamesTheSleep()
    {
        var clock = new SteppedClock();
        var deadline = new BootDeadline(TimeSpan.FromMinutes(5), clock);

        clock.Advance(wall: TimeSpan.FromMinutes(16), awake: TimeSpan.FromSeconds(4));

        _ = await Assert.That(deadline.Expired).IsTrue();
        _ = await Assert.That(deadline.Describe("Umbraco did not reach Run within five minutes."))
            .Contains("machine slept 15 min 56 s during boot");
    }

    [Test]
    public async Task Expired_AfterARealWait_DoesNotNameASleep()
    {
        var clock = new SteppedClock();
        var deadline = new BootDeadline(TimeSpan.FromMinutes(5), clock);

        clock.Advance(wall: TimeSpan.FromMinutes(5), awake: TimeSpan.FromMinutes(5));

        _ = await Assert.That(deadline.Expired).IsTrue();
        _ = await Assert.That(deadline.Describe("Umbraco did not reach Run within five minutes."))
            .IsEqualTo("Umbraco did not reach Run within five minutes.");
    }

    [Test]
    public async Task Expired_BeforeTheLimit_IsFalse()
    {
        var clock = new SteppedClock();
        var deadline = new BootDeadline(TimeSpan.FromMinutes(5), clock);

        clock.Advance(wall: TimeSpan.FromSeconds(30), awake: TimeSpan.FromSeconds(30));

        _ = await Assert.That(deadline.Expired).IsFalse();
    }

    private sealed class SteppedClock : TimeProvider
    {
        private DateTimeOffset wall = new(2026, 11, 18, 18, 0, 0, TimeSpan.Zero);
        private long ticks;

        public override long TimestampFrequency => 1000;

        public override DateTimeOffset GetUtcNow() => wall;

        public override long GetTimestamp() => ticks;

        public void Advance(TimeSpan wall, TimeSpan awake)
        {
            this.wall += wall;
            ticks += (long)awake.TotalMilliseconds;
        }
    }
}
