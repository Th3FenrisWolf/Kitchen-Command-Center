using KCC.Web.Features.Providers;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;

namespace KCC.UnitTests.Features.Providers;

public class AuthorNameProviderTests
{
    [Test]
    [Arguments("Tucker", "Wright", "twright", "Tucker Wright")]
    [Arguments("Tucker", "", "twright", "Tucker")]
    [Arguments("", "Wright", "twright", "Wright")]
    [Arguments("  Tucker  ", "  Wright  ", "twright", "Tucker Wright")]
    [Arguments("", "", "twright", "twright")]
    [Arguments("  ", "  ", "twright", "twright")]
    [Arguments(null, null, "twright", "twright")]
    public async Task FormatDisplayName_PrefersFullNameThenUsername(string first, string last, string userName, string expected)
    {
        _ = await Assert.That(AuthorNameProvider.FormatDisplayName(first, last, userName)).IsEqualTo(expected);
    }

    [Test]
    [Arguments("", "", "")]
    [Arguments("", "", "   ")]
    [Arguments(null, null, null)]
    public async Task FormatDisplayName_ReturnsNullWhenNothingUsable(string first, string last, string userName)
    {
        _ = await Assert.That(AuthorNameProvider.FormatDisplayName(first, last, userName)).IsNull();
    }

    [Test]
    public async Task ResolveMany_EmptyKeys_NeverAsksTheMemberService()
    {
        var members = new Mock<IMemberService>(MockBehavior.Strict);

        var names = await Provider(members).ResolveMany([Guid.Empty]);

        _ = await Assert.That(names.Count).IsEqualTo(0);
    }

    [Test]
    public async Task ResolveMany_NamesAMemberFromFirstAndLastName()
    {
        var key = Guid.NewGuid();
        var members = MembersReturning(Member(key, "Priya", "Balan", "priya.balan"));

        var names = await Provider(members).ResolveMany([key]);

        _ = await Assert.That(names[key]).IsEqualTo("Priya Balan");
    }

    [Test]
    public async Task ResolveMany_SecondCall_IsServedFromTheCache()
    {
        var key = Guid.NewGuid();
        var members = MembersReturning(Member(key, "Priya", "Balan", "priya.balan"));
        var provider = Provider(members);

        _ = await provider.ResolveMany([key]);
        _ = await provider.ResolveMany([key]);

        members.Verify(m => m.GetByKeysAsync(It.IsAny<Guid[]>()), Times.Once);
    }

    [Test]
    public async Task ResolveMany_UnknownMember_HasNoNameAndIsNotLookedUpAgain()
    {
        var key = Guid.NewGuid();
        var members = MembersReturning();
        var provider = Provider(members);

        var names = await provider.ResolveMany([key]);
        _ = await provider.ResolveMany([key]);

        _ = await Assert.That(names.ContainsKey(key)).IsFalse();
        members.Verify(m => m.GetByKeysAsync(It.IsAny<Guid[]>()), Times.Once);
    }

    [Test]
    public async Task Forget_MakesTheNextCallLookTheMemberUpAgain()
    {
        var key = Guid.NewGuid();
        var members = MembersReturning(Member(key, "Priya", "Balan", "priya.balan"));
        var provider = Provider(members);

        _ = await provider.ResolveMany([key]);
        provider.Forget([key]);
        _ = await provider.ResolveMany([key]);

        members.Verify(m => m.GetByKeysAsync(It.IsAny<Guid[]>()), Times.Exactly(2));
    }

    private static AuthorNameProvider Provider(Mock<IMemberService> members) =>
        new(members.Object, new MemoryCache(new MemoryCacheOptions()));

    private static Mock<IMemberService> MembersReturning(params IMember[] found)
    {
        var members = new Mock<IMemberService>();
        members.Setup(m => m.GetByKeysAsync(It.IsAny<Guid[]>()))
            .ReturnsAsync((Guid[] keys) => found.Where(member => keys.Contains(member.Key)));
        return members;
    }

    private static IMember Member(Guid key, string firstName, string lastName, string userName)
    {
        var member = new Mock<IMember>();
        member.SetupGet(m => m.Key).Returns(key);
        member.SetupGet(m => m.Username).Returns(userName);
        member.Setup(m => m.GetValue<string>("firstName", null, null, false)).Returns(firstName);
        member.Setup(m => m.GetValue<string>("lastName", null, null, false)).Returns(lastName);
        return member.Object;
    }
}
