using System.IO;
using System.Text.Encodings.Web;
using KCC.Web.Features.ResourceStringEditing;
using Microsoft.AspNetCore.Mvc.Rendering;
using Xunit;

namespace KCC.Web.Tests.Features.ResourceStringEditing;

public class ResourceStringHtmlExtensionsTests
{
    [Fact]
    public void BuildContent_NotEditable_ReturnsEncodedValueWithNoWrapper()
    {
        var content = ResourceStringHtmlExtensions.BuildContent("Login.Login", "Log in", canEdit: false);

        Assert.Equal("Log in", Render(content));
    }

    [Fact]
    public void BuildContent_NotEditable_HtmlEncodesValue()
    {
        var content = ResourceStringHtmlExtensions.BuildContent("Login.Login", "<script>", canEdit: false);

        Assert.Equal("&lt;script&gt;", Render(content));
    }

    [Fact]
    public void BuildContent_Editable_WrapsInSpanWithClassAndDataAttribute()
    {
        var content = ResourceStringHtmlExtensions.BuildContent("Login.Login", "Log in", canEdit: true);

        Assert.Equal(
            "<span class=\"kcc-rs-editable\" data-resource-key=\"Login.Login\">Log in</span>",
            Render(content));
    }

    [Fact]
    public void BuildContent_Editable_HtmlEncodesValueAndKey()
    {
        var content = ResourceStringHtmlExtensions.BuildContent("X<Y", "<b>", canEdit: true);

        Assert.Equal(
            "<span class=\"kcc-rs-editable\" data-resource-key=\"X&lt;Y\">&lt;b&gt;</span>",
            Render(content));
    }

    private static string Render(Microsoft.AspNetCore.Html.IHtmlContent content)
    {
        using var writer = new StringWriter();
        content.WriteTo(writer, HtmlEncoder.Default);
        return writer.ToString();
    }
}
