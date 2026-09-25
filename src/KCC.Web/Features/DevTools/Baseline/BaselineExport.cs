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
    public async Task<BaselineExportResult> RunAsync()
    {
        if (contentTypeService.Get("recipe") is { } recipeType && contentService.Count(recipeType.Alias) > 0)
        {
            return new(false, "This database holds recipes, which are test data. Export the baseline from a fresh database.");
        }

        var handlers = handlerFactory.GetValidHandlers(new SyncHandlerOptions { Group = "Content", Action = HandlerActions.Export }).ToList();
        var folders = handlers.Select(pair => pair.Handler.DefaultFolder).Distinct().ToList();
        if (folders.Any(string.IsNullOrWhiteSpace))
        {
            return new(false, "A Content-group handler has no default folder, so clearing its folder would empty the whole uSync folder.");
        }

        // An export overwrites files but never removes the files of deleted items, so each folder it writes is cleared first.
        var workingFolder = syncConfig.GetWorkingFolder();
        foreach (var folder in folders)
        {
            var path = Path.Combine(environment.ContentRootPath, workingFolder, folder);
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }

        using var umbracoContext = umbracoContextFactory.EnsureUmbracoContext();

        // ISyncService.StartupExportAsync would also rewrite usync.config with an HMAC of this machine's imaging key.
        var actions = (await syncService.ExportAsync(workingFolder, handlers, callbacks: null)).ToList();
        var failures = actions.Where(action => !action.Success).Select(action => $"{action.Name}: {action.Message}").ToList();

        return failures.Count == 0
            ? new(true, $"Exported {actions.Count} items.")
            : new(false, $"{failures.Count} of {actions.Count} items failed to export: {string.Join("; ", failures)}");
    }
}
