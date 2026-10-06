using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.Navigation;

namespace KCC.IntegrationTests.Config;

// The content service saves a value without the editor's conversion, so these are the stored formats: picks as document
// UDIs, suggestions one to a line, and a block list as its JSON, where a dropdown's or a link picker's value is its array
// as JSON text, as their editors save it. A raw array there fails to convert.
public static class TestSiteSettings
{
    private static readonly string[] NavAliases = ["navMeals", "navDiets", "navQuickPicks", "navSearchSuggestions", "navRecipesNote"];

    public static Guid TaxonomyKey(IServiceProvider services, string folderName, string name)
    {
        var contentService = services.GetRequiredService<IContentService>();
        var folder = contentService.GetRootContent().Single(node => node.Name == folderName);
        services.GetRequiredService<IDocumentNavigationQueryService>().TryGetChildrenKeys(folder.Key, out var children);
        return contentService.GetByIds(children).Single(node => node.Name == name).Key;
    }

    public static string Picks(params Guid[] keys) =>
        string.Join(",", keys.Select(key => Udi.Create(Constants.UdiEntityType.Document, key).ToString()));

    public static QuickPick Preset(string preset, string? label = null) => new(
        "navPreset",
        new Dictionary<string, JsonNode?> { ["preset"] = new JsonArray(preset).ToJsonString(), ["label"] = label });

    public static QuickPick QuickLink(string label, string url) => new(
        "navQuickLink",
        new Dictionary<string, JsonNode?>
        {
            ["label"] = label,
            ["link"] = new JsonArray(new JsonObject
            {
                ["name"] = label,
                ["target"] = null,
                ["unique"] = null,
                ["type"] = null,
                ["udi"] = null,
                ["url"] = url,
                ["queryString"] = null,
                ["culture"] = null,
            }).ToJsonString(),
        });

    public static string QuickPicks(IServiceProvider services, params QuickPick[] picks)
    {
        var contentTypes = services.GetRequiredService<IContentTypeService>();
        var keys = picks.Select(_ => Guid.NewGuid().ToString()).ToList();
        return new JsonObject
        {
            ["contentData"] = new JsonArray(picks.Select((pick, index) => (JsonNode)new JsonObject
            {
                ["contentTypeKey"] = contentTypes.Get(pick.ElementAlias)!.Key.ToString(),
                ["key"] = keys[index],
                ["values"] = new JsonArray(pick.Values.Select(value => (JsonNode)new JsonObject
                {
                    ["alias"] = value.Key,
                    ["culture"] = null,
                    ["editorAlias"] = null,
                    ["segment"] = null,
                    ["value"] = value.Value,
                }).ToArray()),
            }).ToArray()),
            ["settingsData"] = new JsonArray(),
            ["expose"] = new JsonArray(keys.Select(key => (JsonNode)new JsonObject
            {
                ["contentKey"] = key,
                ["culture"] = null,
                ["segment"] = null,
            }).ToArray()),
            ["layout"] = new JsonObject
            {
                ["Umbraco.BlockList"] = new JsonArray(keys.Select(key => (JsonNode)new JsonObject
                {
                    ["contentKey"] = key,
                    ["contentUdi"] = null,
                    ["settingsKey"] = null,
                    ["settingsUdi"] = null,
                }).ToArray()),
            },
        }.ToJsonString();
    }

    public static async Task SetAsync(IServiceProvider services, params (string Alias, object? Value)[] values)
    {
        using var scope = services.CreateScope();
        var contentService = scope.ServiceProvider.GetRequiredService<IContentService>();
        var settings = contentService.GetRootContent().Single(node => node.ContentType.Alias == "siteSettings");
        foreach (var (alias, value) in values)
        {
            settings.SetValue(alias, value);
        }

        await TestContent.SaveAndPublishAsync(scope.ServiceProvider, settings, "Site Settings");
    }

    public static Task ClearNavAsync(IServiceProvider services) =>
        SetAsync(services, NavAliases.Select(alias => (alias, (object?)null)).ToArray());
}

public sealed record QuickPick(string ElementAlias, IReadOnlyDictionary<string, JsonNode?> Values);
