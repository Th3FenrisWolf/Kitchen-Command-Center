using System.Data;
using CMS;
using CMS.Core;
using CMS.DataEngine;
using KCC.Admin.UIPages.Home;
using Microsoft.Extensions.Logging;

[assembly: RegisterImplementation(
    typeof(IFeaturedAppStatsSource),
    typeof(KenticoFeaturedAppStatsSource),
    Lifestyle = Lifestyle.Singleton
)]

namespace KCC.Admin.UIPages.Home;

public sealed class KenticoFeaturedAppStatsSource() : IFeaturedAppStatsSource
{
    // CMS_WebPageItem and CMS_ContentItem don't carry a modified-when column themselves;
    // the timestamp lives on CMS_ContentItemLanguageMetadata (per language). We left-join to
    // it and take MAX(ContentItemLanguageMetadataModifiedWhen) for the most recent edit.

    private const string PageStatsQuery = @"
        SELECT COUNT(*), MAX(m.ContentItemLanguageMetadataModifiedWhen)
        FROM CMS_WebPageItem wp
        LEFT JOIN CMS_ContentItemLanguageMetadata m
            ON m.ContentItemLanguageMetadataContentItemID = wp.WebPageItemContentItemID";

    private const string ContentHubStatsQuery = @"
        SELECT COUNT(*), MAX(m.ContentItemLanguageMetadataModifiedWhen)
        FROM CMS_ContentItem ci
        LEFT JOIN CMS_ContentItemLanguageMetadata m
            ON m.ContentItemLanguageMetadataContentItemID = ci.ContentItemID
        WHERE ci.ContentItemIsReusable = 1";

    public Task<RawStats> GetWebsiteChannelStatsAsync(CancellationToken cancellationToken)
        => GetStatsAsync(PageStatsQuery, cancellationToken);

    public Task<RawStats> GetContentHubStatsAsync(CancellationToken cancellationToken)
        => GetStatsAsync(ContentHubStatsQuery, cancellationToken);

    private static async Task<RawStats> GetStatsAsync(string sql, CancellationToken cancellationToken)
    {
        using var reader = await ConnectionHelper.ExecuteReaderAsync(
            sql,
            parameters: null,
            QueryTypeEnum.SQLQuery,
            CommandBehavior.Default,
            cancellationToken);

        var count = 0;
        DateTimeOffset? lastModified = null;

        if (await reader.ReadAsync(cancellationToken))
        {
            count = reader.GetInt32(0);
            if (!await reader.IsDBNullAsync(1, cancellationToken))
            {
                lastModified = new DateTimeOffset(reader.GetDateTime(1), TimeSpan.Zero);
            }
        }

        return new(count, lastModified);
    }
}
