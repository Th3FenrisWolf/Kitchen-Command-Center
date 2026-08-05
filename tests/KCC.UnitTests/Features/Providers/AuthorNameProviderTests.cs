using CMS.DataEngine;
using CMS.Membership;
using KCC.Web.Features.Providers;
using Moq;

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
    public async Task Resolve_ReturnsNullForEmptyGuidWithoutQuerying()
    {
        var provider = new Mock<IInfoProvider<MemberInfo>>(MockBehavior.Strict);
        var resolver = new AuthorNameProvider(provider.Object);

        var name = await resolver.Resolve(Guid.Empty);

        _ = await Assert.That(name).IsNull();
        provider.Verify(p => p.Get(), Times.Never);
    }

    [Test]
    public async Task ResolveMany_ReturnsEmptyForNoUsableGuidsWithoutQuerying()
    {
        var provider = new Mock<IInfoProvider<MemberInfo>>(MockBehavior.Strict);
        var resolver = new AuthorNameProvider(provider.Object);

        var names = await resolver.ResolveMany([Guid.Empty]);

        _ = await Assert.That(names.Count()).IsEqualTo(0);
        provider.Verify(p => p.Get(), Times.Never);
    }

    [Test]
    public async Task ResolveMany_ReturnsEmptyForEmptyInputWithoutQuerying()
    {
        var provider = new Mock<IInfoProvider<MemberInfo>>(MockBehavior.Strict);
        var resolver = new AuthorNameProvider(provider.Object);

        var names = await resolver.ResolveMany([]);

        _ = await Assert.That(names.Count()).IsEqualTo(0);
        provider.Verify(p => p.Get(), Times.Never);
    }
}
