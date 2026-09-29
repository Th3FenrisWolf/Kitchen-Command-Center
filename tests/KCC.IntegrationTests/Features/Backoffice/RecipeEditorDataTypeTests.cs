using KCC.IntegrationTests.Config;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Services;

namespace KCC.IntegrationTests.Features.Backoffice;

// The schema stays a text area or text box, so the stored JSON and the generated string properties do not change;
// only the backoffice editor does.
public class RecipeEditorDataTypeTests
{
    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    [Test]
    [Arguments("KCC Ingredients", "Umbraco.TextArea", "KCC.PropertyEditorUi.Ingredients")]
    [Arguments("KCC Instructions", "Umbraco.TextArea", "KCC.PropertyEditorUi.Instructions")]
    [Arguments("KCC Recipe Icon", "Umbraco.TextBox", "KCC.PropertyEditorUi.RecipeIcon")]
    public async Task RecipeDataType_UsesTheKccEditor(string name, string schema, string editorUi)
    {
        var dataType = (await Site.Services.GetRequiredService<IDataTypeService>().GetAllAsync()).Single(type => type.Name == name);

        _ = await Assert.That(dataType.EditorAlias).IsEqualTo(schema);
        _ = await Assert.That(dataType.EditorUiAlias).IsEqualTo(editorUi);
    }
}
