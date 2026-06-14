using System;
using KCC.Web.Features.Pages.Account;
using Xunit;

namespace KCC.Web.Tests.Features.Pages.Account;

public class AccountViewModelTests
{
    [Theory]
    [InlineData("Alex", "Carter", "AC")]
    [InlineData("alex", "carter", "AC")]
    [InlineData("Alex", "", "A")]
    [InlineData("  Alex  ", "  Carter  ", "AC")]
    public void ComputeInitials_UsesNames(string first, string last, string expected)
    {
        Assert.Equal(expected, AccountViewModel.ComputeInitials(first, last, "ignored"));
    }

    [Theory]
    [InlineData("", "", "alex.carter@example.com", "AL")]
    [InlineData(null, null, "a", "A")]
    [InlineData("", "", "", "")]
    public void ComputeInitials_FallsBackWhenNamesEmpty(string first, string last, string fallback, string expected)
    {
        Assert.Equal(expected, AccountViewModel.ComputeInitials(first, last, fallback));
    }

    [Fact]
    public void FormatMemberSince_FormatsMonthAndYear()
    {
        Assert.Equal("June 2024", AccountViewModel.FormatMemberSince(new DateTime(2024, 6, 15)));
    }

    [Fact]
    public void FormatMemberSince_ReturnsEmptyForNull()
    {
        Assert.Equal(string.Empty, AccountViewModel.FormatMemberSince(null));
    }
}
