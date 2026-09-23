using System.Data;
using CMS.DataEngine;

namespace KCC.Contributions.Admin.Overview;

public sealed record RecipeRollupQuery(int Page, int PageSize, string SearchText, double? MaxAverageRating);

public sealed record RecipeRollupRow(
    Guid RecipeGuid,
    string DisplayName,
    double? AverageRating,
    int ReviewCount,
    int NoteCount,
    int CookedCount,
    DateTime? LastActivity);

public sealed record RecipeRollupPage(IReadOnlyList<RecipeRollupRow> Rows, int TotalCount);

public interface IRecipeRollupSource
{
    Task<RecipeRollupPage> GetPageAsync(RecipeRollupQuery query, CancellationToken cancellationToken);
}

/// <summary>Cross-recipe rollup of all three contribution tables in one grouped query, paged SQL-side.</summary>
internal sealed class SqlRecipeRollupSource : IRecipeRollupSource
{
    private const string BaseQuery = @"
        ;WITH PerTable AS (
            SELECT RecipeGuid, SUM(CAST(Rating AS float)) AS RatingSum, COUNT(*) AS ReviewCount,
                   0 AS NoteCount, 0 AS CookedCount, MAX(ReviewCreated) AS LastActivity
            FROM KCC_VariantReview GROUP BY RecipeGuid
            UNION ALL
            SELECT RecipeGuid, 0, 0, COUNT(*), 0, MAX(NoteCreated)
            FROM KCC_VariantCookNote GROUP BY RecipeGuid
            UNION ALL
            SELECT RecipeGuid, 0, 0, 0, COUNT(*), MAX(CookedCreated)
            FROM KCC_VariantCooked GROUP BY RecipeGuid
        ),
        Rollup AS (
            SELECT RecipeGuid, SUM(RatingSum) AS RatingSum, SUM(ReviewCount) AS ReviewCount,
                   SUM(NoteCount) AS NoteCount, SUM(CookedCount) AS CookedCount, MAX(LastActivity) AS LastActivity
            FROM PerTable GROUP BY RecipeGuid
        ),
        Named AS (
            SELECT r.RecipeGuid,
                   CASE WHEN r.ReviewCount > 0 THEN r.RatingSum / r.ReviewCount END AS AverageRating,
                   r.ReviewCount, r.NoteCount, r.CookedCount, r.LastActivity, n.DisplayName
            FROM Rollup r
            OUTER APPLY (
                SELECT TOP 1 m.ContentItemLanguageMetadataDisplayName AS DisplayName
                FROM CMS_ContentItem ci
                INNER JOIN CMS_ContentItemLanguageMetadata m
                    ON m.ContentItemLanguageMetadataContentItemID = ci.ContentItemID
                LEFT JOIN CMS_ContentLanguage l
                    ON l.ContentLanguageID = m.ContentItemLanguageMetadataContentLanguageID
                WHERE ci.ContentItemGUID = r.RecipeGuid
                ORDER BY CASE WHEN l.ContentLanguageIsDefault = 1 THEN 0 ELSE 1 END,
                         m.ContentItemLanguageMetadataContentLanguageID
            ) n
        )
        SELECT RecipeGuid, DisplayName, AverageRating, ReviewCount, NoteCount, CookedCount, LastActivity,
               COUNT(*) OVER () AS TotalCount
        FROM Named";

    private const string OrderAndPageClause = @"
        ORDER BY LastActivity DESC, RecipeGuid
        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

    public async Task<RecipeRollupPage> GetPageAsync(RecipeRollupQuery query, CancellationToken cancellationToken)
    {
        var conditions = new List<string>();
        var parameters = new QueryDataParameters
        {
            { "@Offset", Math.Max(0, query.Page) * Math.Max(1, query.PageSize) },
            { "@PageSize", Math.Max(1, query.PageSize) },
        };

        if (!string.IsNullOrWhiteSpace(query.SearchText))
        {
            conditions.Add(@"DisplayName LIKE @SearchPattern ESCAPE '\'");
            parameters.Add("@SearchPattern", $"%{EscapeLikePattern(query.SearchText.Trim())}%");
        }

        if (query.MaxAverageRating is { } maxAverageRating)
        {
            conditions.Add("AverageRating <= @MaxAverageRating");
            parameters.Add("@MaxAverageRating", maxAverageRating);
        }

        var sql = BaseQuery
            + (conditions.Count > 0 ? $"\n        WHERE {string.Join(" AND ", conditions)}" : string.Empty)
            + OrderAndPageClause;

        using var reader = await ConnectionHelper.ExecuteReaderAsync(
            sql,
            parameters,
            QueryTypeEnum.SQLQuery,
            CommandBehavior.Default,
            cancellationToken);

        var rows = new List<RecipeRollupRow>();
        var totalCount = 0;

        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new RecipeRollupRow(
                reader.GetGuid(0),
                await reader.IsDBNullAsync(1, cancellationToken) ? null : reader.GetString(1),
                await reader.IsDBNullAsync(2, cancellationToken) ? null : reader.GetDouble(2),
                reader.GetInt32(3),
                reader.GetInt32(4),
                reader.GetInt32(5),
                await reader.IsDBNullAsync(6, cancellationToken) ? null : reader.GetDateTime(6)));
            totalCount = reader.GetInt32(7);
        }

        return new RecipeRollupPage(rows, totalCount);
    }

    /// <summary>Escapes LIKE wildcards so user input matches literally.</summary>
    internal static string EscapeLikePattern(string text) =>
        text.Replace(@"\", @"\\")
            .Replace("%", @"\%")
            .Replace("_", @"\_")
            .Replace("[", @"\[");
}
