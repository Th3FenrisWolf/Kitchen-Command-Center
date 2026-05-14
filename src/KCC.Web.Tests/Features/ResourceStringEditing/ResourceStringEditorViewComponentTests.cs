using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using KCC.Web.Features.ResourceStringEditing;
using Kentico.Content.Web.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Xunit;

namespace KCC.Web.Tests.Features.ResourceStringEditing;

public class ResourceStringEditorViewComponentTests
{
    [Fact]
    public async Task InvokeAsync_CannotEdit_ReturnsEmptyContent()
    {
        var sut = new ResourceStringEditorViewComponent(
            new StubEditorAccess(canEdit: false, isPreview: false),
            new StubPreferredLanguageRetriever("en"),
            new StubContentLanguageRepository());

        var result = await sut.InvokeAsync();

        var content = Assert.IsType<ContentViewComponentResult>(result);
        Assert.Equal(string.Empty, content.Content);
    }

    [Fact]
    public async Task InvokeAsync_CanEdit_ReturnsViewResultWithSerializedContext()
    {
        var languages = new[]
        {
            new ContentLanguageOption("en", "English"),
            new ContentLanguageOption("es", "Spanish"),
        };
        var sut = new ResourceStringEditorViewComponent(
            new StubEditorAccess(canEdit: true, isPreview: true),
            new StubPreferredLanguageRetriever("es"),
            new StubContentLanguageRepository(languages));

        var result = await sut.InvokeAsync();

        var view = Assert.IsType<ViewViewComponentResult>(result);
        var model = Assert.IsType<ResourceStringEditorViewModel>(view.ViewData.Model);

        using var doc = JsonDocument.Parse(model.SerializedContext);
        var root = doc.RootElement;
        Assert.Equal("es", root.GetProperty("currentLanguage").GetString());
        Assert.True(root.GetProperty("isPreviewMode").GetBoolean());

        var available = root.GetProperty("availableLanguages").EnumerateArray().ToList();
        Assert.Equal(2, available.Count);
        Assert.Equal("en", available[0].GetProperty("code").GetString());
        Assert.Equal("English", available[0].GetProperty("name").GetString());
        Assert.Equal("es", available[1].GetProperty("code").GetString());
        Assert.Equal("Spanish", available[1].GetProperty("name").GetString());
    }

    [Fact]
    public async Task InvokeAsync_CanEdit_IsPreviewModeFalse_SerializesFalse()
    {
        var sut = new ResourceStringEditorViewComponent(
            new StubEditorAccess(canEdit: true, isPreview: false),
            new StubPreferredLanguageRetriever("en"),
            new StubContentLanguageRepository());

        var result = await sut.InvokeAsync();

        var view = Assert.IsType<ViewViewComponentResult>(result);
        var model = Assert.IsType<ResourceStringEditorViewModel>(view.ViewData.Model);

        using var doc = JsonDocument.Parse(model.SerializedContext);
        Assert.False(doc.RootElement.GetProperty("isPreviewMode").GetBoolean());
    }

    private sealed class StubEditorAccess(bool canEdit, bool isPreview) : IResourceStringEditorAccess
    {
        public bool CanEdit() => canEdit;

        public bool IsPreviewMode() => isPreview;
    }

    private sealed class StubPreferredLanguageRetriever(string language) : IPreferredLanguageRetriever
    {
        public string Get() => language;
    }

    private sealed class StubContentLanguageRepository(params ContentLanguageOption[] languages)
        : IContentLanguageRepository
    {
        public Task<bool> ExistsAsync(string code) => Task.FromResult(languages.Any(l => l.Code == code));

        public IReadOnlyList<ContentLanguageOption> ListAll() => languages;
    }
}
