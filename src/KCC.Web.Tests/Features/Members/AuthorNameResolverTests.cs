using CMS.DataEngine;
using CMS.Membership;
using KCC.Web.Features.Members;
using Moq;
using Xunit;

namespace KCC.Web.Tests.Features.Members;

public class AuthorNameResolverTests
{
    [Theory]
    [InlineData("Tucker", "Wright", "twright", "Tucker Wright")]
    [InlineData("Tucker", "", "twright", "Tucker")]
    [InlineData("", "Wright", "twright", "Wright")]
    [InlineData("  Tucker  ", "  Wright  ", "twright", "Tucker Wright")]
    [InlineData("", "", "twright", "twright")]
    [InlineData("  ", "  ", "twright", "twright")]
    [InlineData(null, null, "twright", "twright")]
    public void FormatDisplayName_PrefersFullNameThenUsername(string first, string last, string userName, string expected)
    {
        Assert.Equal(expected, AuthorNameResolver.FormatDisplayName(first, last, userName));
    }

    [Theory]
    [InlineData("", "", "")]
    [InlineData("", "", "   ")]
    [InlineData(null, null, null)]
    public void FormatDisplayName_ReturnsNullWhenNothingUsable(string first, string last, string userName)
    {
        Assert.Null(AuthorNameResolver.FormatDisplayName(first, last, userName));
    }

    [Fact]
    public async Task Resolve_ReturnsNullForEmptyGuidWithoutQuerying()
    {
        var provider = new Mock<IInfoProvider<MemberInfo>>(MockBehavior.Strict);
        var resolver = new AuthorNameResolver(provider.Object);

        var name = await resolver.Resolve(Guid.Empty);

        Assert.Null(name);
        provider.Verify(p => p.Get(), Times.Never);
    }

    [Fact]
    public async Task ResolveMany_ReturnsEmptyForNoUsableGuidsWithoutQuerying()
    {
        var provider = new Mock<IInfoProvider<MemberInfo>>(MockBehavior.Strict);
        var resolver = new AuthorNameResolver(provider.Object);

        var names = await resolver.ResolveMany([Guid.Empty]);

        Assert.Empty(names);
        provider.Verify(p => p.Get(), Times.Never);
    }

    [Fact]
    public async Task ResolveMany_ReturnsEmptyForEmptyInputWithoutQuerying()
    {
        var provider = new Mock<IInfoProvider<MemberInfo>>(MockBehavior.Strict);
        var resolver = new AuthorNameResolver(provider.Object);

        var names = await resolver.ResolveMany([]);

        Assert.Empty(names);
        provider.Verify(p => p.Get(), Times.Never);
    }
}
