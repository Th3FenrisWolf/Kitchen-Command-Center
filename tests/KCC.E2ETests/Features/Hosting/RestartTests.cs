using KCC.E2ETests.Config;

namespace KCC.E2ETests.Features.Hosting;

[NotInParallel]
public class RestartTests
{
    private const string DictionaryKey = "Login.SignIn";
    private const string EditedValue = "Edited on the live site";
    private const string BaselineDocument = "Recipe Tags";
    private const string EditedDocumentName = "Tags renamed on the live site";

    [Test]
    public async Task LiveEdits_SurviveARestart()
    {
        await using var site = new SiteProcess(withSsr: false);
        await site.InitializeAsync();
        await site.StopAsync();

        var imported = await SiteDatabase.ReadDictionaryValueAsync(site.DatabasePath, DictionaryKey);
        await SiteDatabase.WriteDictionaryValueAsync(site.DatabasePath, DictionaryKey, EditedValue);
        var documentId = await SiteDatabase.FindDocumentIdAsync(site.DatabasePath, BaselineDocument);
        await SiteDatabase.RenameDocumentAsync(site.DatabasePath, documentId, EditedDocumentName);

        await site.StartAsync();
        await site.StopAsync();

        _ = await Assert.That(imported).IsNotNull();
        _ = await Assert.That(imported).IsNotEqualTo(EditedValue);
        _ = await Assert.That(await SiteDatabase.ReadDictionaryValueAsync(site.DatabasePath, DictionaryKey)).IsEqualTo(EditedValue);
        _ = await Assert.That(await SiteDatabase.ReadDocumentNameAsync(site.DatabasePath, documentId)).IsEqualTo(EditedDocumentName);
    }
}
