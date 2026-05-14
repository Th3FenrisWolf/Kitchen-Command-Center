#nullable enable

using System.Text.Json.Nodes;
using CMS.ContentEngine;
using CMS.DataEngine;
using Microsoft.AspNetCore.Mvc;

namespace KCC.Web.Features.Sections.BentoBox;

[ApiController]
[Route("api/bento-box")]
public sealed class BentoBoxEditorController(
    IInfoProvider<ContentLanguageInfo> languageProvider)
    : ControllerBase
{
    [HttpPut("property")]
    public ActionResult UpdateProperty([FromBody] BentoBoxPropertyUpdateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.LanguageName) || request.PageId <= 0)
        {
            return BadRequest(new { error = "pageId and languageName are required" });
        }

        var contentItemId = GetContentItemId(request.PageId);
        if (contentItemId is null)
        {
            return NotFound(new { error = $"Page not found: {request.PageId}" });
        }

        var languageId = languageProvider.Get()
            .WhereEquals(nameof(ContentLanguageInfo.ContentLanguageName), request.LanguageName)
            .Column(nameof(ContentLanguageInfo.ContentLanguageID))
            .TopN(1)
            .GetScalarResult(0);

        if (languageId is 0)
        {
            return BadRequest(new { error = $"Unknown language: {request.LanguageName}" });
        }

        var (commonDataId, json) = ReadPageBuilderJson(contentItemId.Value, languageId);
        if (commonDataId is null || string.IsNullOrWhiteSpace(json))
        {
            return NotFound(new { error = "No page builder data found for this page/language" });
        }

        var root = JsonNode.Parse(json);
        var editableAreas = root?["editableAreas"]?.AsArray();
        if (editableAreas is null)
        {
            return BadRequest(new { error = "Invalid page builder JSON" });
        }

        bool updated = false;
        foreach (var area in editableAreas)
        {
            var sections = area?["sections"]?.AsArray();
            if (sections is null)
            {
                continue;
            }

            foreach (var section in sections)
            {
                if (section?["type"]?.GetValue<string>() != request.SectionType)
                {
                    continue;
                }

                var properties = section!["properties"];
                if (properties is null)
                {
                    continue;
                }

                properties[request.PropertyName] = request.Value;

                if (request.PropertyName == "gridLayout")
                {
                    SyncZones(section!, request.Value);
                }

                updated = true;
                break;
            }

            if (updated)
            {
                break;
            }
        }

        if (!updated)
        {
            return NotFound(new { error = $"No section of type '{request.SectionType}' found" });
        }

        WritePageBuilderJson(commonDataId.Value, root!.ToJsonString());

        return Ok(new { success = true });
    }

    private static void SyncZones(JsonNode section, string gridLayoutJson)
    {
        var layout = BentoBoxGridLayout.Parse(gridLayoutJson);
        var cells = layout.BuildCells();

        var zones = section["zones"]?.AsArray() ?? new JsonArray();
        section["zones"] = zones;

        var existingByName = new Dictionary<string, JsonNode>();
        foreach (var zone in zones)
        {
            var name = zone?["name"]?.GetValue<string>();
            if (name is not null && zone is not null)
            {
                existingByName[name] = zone;
            }
        }

        var newZones = new JsonArray();
        foreach (var cell in cells)
        {
            if (existingByName.TryGetValue(cell.ZoneName, out var existing))
            {
                var detached = existing.DeepClone();
                newZones.Add(detached);
            }
            else
            {
                newZones.Add(new JsonObject
                {
                    ["identifier"] = Guid.NewGuid().ToString(),
                    ["name"] = cell.ZoneName,
                    ["widgets"] = new JsonArray(),
                });
            }
        }

        section["zones"] = newZones;
    }

    private static int? GetContentItemId(int webPageItemId)
    {
        var parameters = new QueryDataParameters();
        parameters.Add("@PageId", webPageItemId);

        var ds = ConnectionHelper.ExecuteQuery(
            "SELECT TOP 1 WebPageItemContentItemID FROM CMS_WebPageItem WHERE WebPageItemID = @PageId",
            parameters,
            QueryTypeEnum.SQLQuery);

        if (ds.Tables[0].Rows.Count == 0)
        {
            return null;
        }

        return (int)ds.Tables[0].Rows[0]["WebPageItemContentItemID"];
    }

    private static (int? id, string? json) ReadPageBuilderJson(int contentItemId, int languageId)
    {
        var parameters = new QueryDataParameters();
        parameters.Add("@ContentItemID", contentItemId);
        parameters.Add("@LanguageID", languageId);

        var ds = ConnectionHelper.ExecuteQuery(
            "SELECT TOP 1 ContentItemCommonDataID, ContentItemCommonDataVisualBuilderWidgets " +
            "FROM CMS_ContentItemCommonData " +
            "WHERE ContentItemCommonDataContentItemID = @ContentItemID " +
            "AND ContentItemCommonDataContentLanguageID = @LanguageID " +
            "AND ContentItemCommonDataIsLatest = 1",
            parameters,
            QueryTypeEnum.SQLQuery);

        if (ds.Tables[0].Rows.Count == 0)
        {
            return (null, null);
        }

        var row = ds.Tables[0].Rows[0];
        return (
            (int)row["ContentItemCommonDataID"],
            row["ContentItemCommonDataVisualBuilderWidgets"]?.ToString());
    }

    private static void WritePageBuilderJson(int commonDataId, string json)
    {
        var parameters = new QueryDataParameters();
        parameters.Add("@Json", json);
        parameters.Add("@Id", commonDataId);

        ConnectionHelper.ExecuteNonQuery(
            "UPDATE CMS_ContentItemCommonData " +
            "SET ContentItemCommonDataVisualBuilderWidgets = @Json " +
            "WHERE ContentItemCommonDataID = @Id",
            parameters,
            QueryTypeEnum.SQLQuery);
    }
}

public record BentoBoxPropertyUpdateRequest(
    int PageId,
    string LanguageName,
    string SectionType,
    string PropertyName,
    string Value);
