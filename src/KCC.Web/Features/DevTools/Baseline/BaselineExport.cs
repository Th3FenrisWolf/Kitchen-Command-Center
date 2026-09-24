using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using uSync.BackOffice;
using uSync.BackOffice.Configuration;
using uSync.BackOffice.SyncHandlers;
using uSync.BackOffice.SyncHandlers.Models;

namespace KCC.Web.Features.DevTools.Baseline;

public sealed record BaselineExportResult(bool Succeeded, string Message);

public class BaselineExport(
    ISyncService syncService,
    ISyncHandlerFactory handlerFactory,
    ISyncConfigService syncConfig,
    IContentService contentService,
    IContentTypeService contentTypeService,
    IUmbracoContextFactory umbracoContextFactory,
    IWebHostEnvironment environment)
{
    // The handler folders of uSync's Content group. An export overwrites files but never removes the files of
    // deleted items, so the folders are cleared first.
    private static readonly string[] ContentGroupFolders = ["Content", "Media", "Domains", "Blueprints", "RelationTypes"];

    public async Task<BaselineExportResult> RunAsync()
    {
        if (contentTypeService.Get("recipe") is { } recipeType && contentService.Count(recipeType.Alias) > 0)
        {
            return new(false, "This database holds recipes, which are test data. Export the baseline from a fresh database.");
        }

        var workingFolder = syncConfig.GetWorkingFolder();
        foreach (var folder in ContentGroupFolders)
        {
            var path = Path.Combine(environment.ContentRootPath, workingFolder, folder);
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }

        using var umbracoContext = umbracoContextFactory.EnsureUmbracoContext();

        // ISyncService.StartupExportAsync would also rewrite usync.config with an HMAC of this machine's imaging key.
        var handlers = handlerFactory.GetValidHandlers(new SyncHandlerOptions { Group = "Content", Action = HandlerActions.Export });
        var actions = await syncService.ExportAsync(workingFolder, handlers, callbacks: null);
        return new(true, $"Exported {actions.Count()} items.");
    }
}
