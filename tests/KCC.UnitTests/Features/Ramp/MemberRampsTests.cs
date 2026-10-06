using KCC.Web.Features.Ramp;
using KCC.Web.Features.Sqlite;
using Moq;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;

namespace KCC.UnitTests.Features.Ramp;

public class MemberRampsTests
{
    [Test]
    public async Task SaveAsync_ReadsAndSavesTheMemberInsideTheMemberWriteLock()
    {
        var key = Guid.NewGuid();
        var writeLock = new WatchedLock();
        var member = new Mock<IMember>();
        var members = new Mock<IMemberService>();
        var readInside = false;
        var savedInside = false;
        string stored = null;
        members.Setup(service => service.GetById(key)).Returns(() =>
        {
            readInside = writeLock.Held;
            return member.Object;
        });
        members.Setup(service => service.Save(member.Object, It.IsAny<int>())).Callback(() => savedInside = writeLock.Held);
        member.Setup(m => m.SetValue("ramp", It.IsAny<object>(), null, null))
            .Callback<string, object, string, string>((_, value, _, _) => stored = (string)value);

        var saved = await new MemberRamps(members.Object, writeLock).SaveAsync(key, Ramps.Dark);

        _ = await Assert.That(saved).IsTrue();
        _ = await Assert.That(stored).IsEqualTo("[\"Dark\"]");
        _ = await Assert.That(readInside).IsTrue();
        _ = await Assert.That(savedInside).IsTrue();
    }

    [Test]
    public async Task SaveAsync_ForAMemberThatIsGone_SavesNothing()
    {
        var members = new Mock<IMemberService>();

        var saved = await new MemberRamps(members.Object, new WatchedLock()).SaveAsync(Guid.NewGuid(), Ramps.Dark);

        _ = await Assert.That(saved).IsFalse();
        members.Verify(service => service.Save(It.IsAny<IMember>(), It.IsAny<int>()), Times.Never);
    }

    [Test]
    public async Task SavedFor_ReadsTheMembersRamp_AndDeviceForNoOne()
    {
        var member = new Mock<IMember>();
        member.Setup(m => m.GetValue<string>("ramp", null, null, false)).Returns("[\"Light\"]");
        var members = new Mock<IMemberService>();
        members.Setup(service => service.GetByUsername("ada")).Returns(member.Object);
        var ramps = new MemberRamps(members.Object, new WatchedLock());

        _ = await Assert.That(ramps.SavedFor("ada")).IsEqualTo(Ramps.Light);
        _ = await Assert.That(ramps.SavedFor("nobody")).IsEqualTo(Ramps.Device);
    }

    private sealed class WatchedLock : IMemberWriteLock
    {
        public bool Held { get; private set; }

        public Task RunAsync(Func<Task> write) => RunAsync(async () =>
        {
            await write();
            return true;
        });

        public async Task<T> RunAsync<T>(Func<Task<T>> write)
        {
            Held = true;
            try
            {
                return await write();
            }
            finally
            {
                Held = false;
            }
        }
    }
}
