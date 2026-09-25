using KCC.Web.Features.DevTools.Baseline;
using Microsoft.AspNetCore.Hosting;
using Moq;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using uSync.BackOffice;
using uSync.BackOffice.Configuration;
using uSync.BackOffice.SyncHandlers;
using uSync.BackOffice.SyncHandlers.Interfaces;
using uSync.BackOffice.SyncHandlers.Models;

namespace KCC.UnitTests.Features.DevTools.Baseline;

public sealed class BaselineExportTests : IDisposable
{
    private const string WorkingFolder = "uSync/v17/";

    private readonly string contentRoot = Directory.CreateTempSubdirectory("kcc-unit-").FullName;
    private readonly Mock<ISyncService> syncService = new();

    public void Dispose() => Directory.Delete(contentRoot, recursive: true);

    [Test]
    public async Task RunAsync_ClearsExactlyTheFoldersItsHandlersWrite()
    {
        foreach (var folder in new[] { "Content", "Custom", "Media", "ContentTypes" })
        {
            WriteStaleFile(folder);
        }

        var result = await Export([], Handler("Content"), Handler("Custom")).RunAsync();

        _ = await Assert.That(result.Succeeded).IsTrue();
        _ = await Assert.That(FolderExists("Content")).IsFalse();
        _ = await Assert.That(FolderExists("Custom")).IsFalse();
        _ = await Assert.That(FolderExists("Media")).IsTrue();
        _ = await Assert.That(FolderExists("ContentTypes")).IsTrue();
    }

    [Test]
    public async Task RunAsync_WhenAnItemFailsToExport_ReportsTheFailure()
    {
        uSyncAction[] actions =
        [
            new() { Success = true, Name = "Recipes" },
            new() { Success = false, Name = "Home", Message = "Serializer failed" },
        ];

        var result = await Export(actions, Handler("Content")).RunAsync();

        _ = await Assert.That(result.Succeeded).IsFalse();
        _ = await Assert.That(result.Message).Contains("Home: Serializer failed");
    }

    [Test]
    public async Task RunAsync_WhenAHandlerHasNoFolder_ClearsAndExportsNothing()
    {
        WriteStaleFile("Content");

        var result = await Export([], Handler("Content"), Handler(" ")).RunAsync();

        _ = await Assert.That(result.Succeeded).IsFalse();
        _ = await Assert.That(FolderExists("Content")).IsTrue();
        syncService.Verify(
            service => service.ExportAsync(It.IsAny<string>(), It.IsAny<IEnumerable<HandlerConfigPair>>(), It.IsAny<uSyncCallbacks>()),
            Times.Never());
    }

    private static ISyncHandler Handler(string defaultFolder) =>
        Mock.Of<ISyncHandler>(handler => handler.DefaultFolder == defaultFolder);

    private BaselineExport Export(uSyncAction[] actions, params ISyncHandler[] handlers)
    {
        var pairs = handlers.Select(handler => new HandlerConfigPair { Handler = handler, Settings = new HandlerSettings() }).ToList();

        var handlerFactory = new Mock<ISyncHandlerFactory>();
        handlerFactory
            .Setup(factory => factory.GetValidHandlers(It.Is<SyncHandlerOptions>(options => options.Group == "Content" && options.Action == HandlerActions.Export)))
            .Returns(pairs);

        syncService
            .Setup(service => service.ExportAsync(WorkingFolder, It.Is<IEnumerable<HandlerConfigPair>>(run => run.SequenceEqual(pairs)), It.IsAny<uSyncCallbacks>()))
            .ReturnsAsync(actions);

        var syncConfig = new Mock<ISyncConfigService>();
        syncConfig.Setup(config => config.GetWorkingFolder()).Returns(WorkingFolder);

        var environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(env => env.ContentRootPath).Returns(contentRoot);

        return new BaselineExport(
            syncService.Object,
            handlerFactory.Object,
            syncConfig.Object,
            Mock.Of<IContentService>(),
            Mock.Of<IContentTypeService>(),
            Mock.Of<IUmbracoContextFactory>(),
            environment.Object);
    }

    private void WriteStaleFile(string folder)
    {
        var path = Path.Combine(contentRoot, WorkingFolder, folder);
        Directory.CreateDirectory(path);
        File.WriteAllText(Path.Combine(path, "stale.config"), "<Stale />");
    }

    private bool FolderExists(string folder) => Directory.Exists(Path.Combine(contentRoot, WorkingFolder, folder));
}
