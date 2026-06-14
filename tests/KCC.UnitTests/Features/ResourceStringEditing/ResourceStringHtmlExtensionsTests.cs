using System.Text.Encodings.Web;
using KCC.ResourceStrings.Editing;

namespace KCC.UnitTests.Features.ResourceStringEditing;

public class ResourceStringHtmlExtensionsTests
{
    [Test]
    public async Task BuildContent_NotEditable_WrapsEncodedValueInPlainSpan()
    {
        var content = ResourceStringHtmlExtensions.BuildContent("Login.Login", "Log in", canEdit: false);

        _ = await Assert.That(Render(content)).IsEqualTo("<span>Log in</span>");
    }

    [Test]
    public async Task BuildContent_NotEditable_HtmlEncodesValue()
    {
        var content = ResourceStringHtmlExtensions.BuildContent("Login.Login", "<script>", canEdit: false);

        _ = await Assert.That(Render(content)).IsEqualTo("<span>&lt;script&gt;</span>");
    }

    [Test]
    public async Task BuildContent_Editable_WrapsInSpanWithClassAndDataAttribute()
    {
        var content = ResourceStringHtmlExtensions.BuildContent("Login.Login", "Log in", canEdit: true);

        _ = await Assert.That(Render(content)).IsEqualTo(
            "<span class=\"kcc-rs-editable\" data-resource-key=\"Login.Login\">Log in</span>");
    }

    [Test]
    public async Task BuildContent_Editable_HtmlEncodesValueAndKey()
    {
        var content = ResourceStringHtmlExtensions.BuildContent("X<Y", "<b>", canEdit: true);

        _ = await Assert.That(Render(content)).IsEqualTo(
            "<span class=\"kcc-rs-editable\" data-resource-key=\"X&lt;Y\">&lt;b&gt;</span>");
    }

    private static string Render(Microsoft.AspNetCore.Html.IHtmlContent content)
    {
        using var writer = new StringWriter();
        content.WriteTo(writer, HtmlEncoder.Default);
        return writer.ToString();
    }
}
