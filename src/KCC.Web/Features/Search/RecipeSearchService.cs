using System.Globalization;
using Kentico.Xperience.Lucene.Core.Indexing;
using Kentico.Xperience.Lucene.Core.Search;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Documents;
using Lucene.Net.Facet;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;

namespace KCC.Web.Features.Search;

/// <summary>
/// Queries the recipe Lucene index. Builds a boosted full-text query (optionally intersected with a prep+cook time
/// range), applies the selected category/diet facets as a drill-down, and reads cross-aware (drill-sideways) facet
/// counts alongside a sorted, paged slice of hits and an optional highest-rated spotlight.
/// </summary>
public class RecipeSearchService(
    ILuceneIndexManager indexManager,
    ILuceneSearchService searchService) : IRecipeSearchService
{
    // Suffix under which the average rating is stored (the un-suffixed key is the sort-only DoubleDocValuesField).
    private const string AverageRatingStoredSuffix = "_v";

    // Matches on the recipe name count for more than matches buried in the combined content blob.
    private const float NameBoost = 2f;

    // Category and diet are small controlled taxonomies; this comfortably returns every value with its count.
    private const int FacetTopN = 1000;

    /// <summary>Runs a recipe search for the supplied criteria.</summary>
    /// <param name="rawCriteria">The search criteria; it is normalized before use.</param>
    /// <returns>The requested page of hits together with facet counts and the optional spotlight recipe.</returns>
    public RecipeSearchResults Search(RecipeSearchCriteria rawCriteria)
    {
        var criteria = rawCriteria.Normalized();
        var index = indexManager.GetRequiredIndex(RecipeSearchConstants.IndexName);
        var facetsConfig = BuildFacetsConfig();
        var baseQuery = BuildQuery(criteria);

        var drill = new DrillDownQuery(facetsConfig, baseQuery);
        foreach (var category in criteria.Categories.Where(value => !string.IsNullOrWhiteSpace(value)))
        {
            drill.Add(RecipeSearchConstants.FacetCategory, category);
        }

        foreach (var diet in criteria.Diets.Where(value => !string.IsNullOrWhiteSpace(value)))
        {
            drill.Add(RecipeSearchConstants.FacetDiet, diet);
        }

        var sort = BuildSort(criteria);

        return searchService.UseSearcherWithDrillSideways(index, (searcher, drillSideways) =>
        {
            // Request the whole index so the returned TopDocs holds every match: both paging and the spotlight
            // scan need the complete hit set, and DrillSideways clamps the requested count to MaxDoc anyway.
            var topN = Math.Max(searcher.IndexReader.MaxDoc, 1);
            var outcome = drillSideways.Search(drill, null, null, topN, sort, true, false);
            var scoreDocs = outcome.Hits.ScoreDocs;

            RecipeSearchHit spotlight = null;
            List<RecipeSearchHit> results;

            if (criteria.Page == 0)
            {
                // Page zero needs the full set for the spotlight scan, so map every hit once and reuse it.
                var all = new List<RecipeSearchHit>(scoreDocs.Length);
                foreach (var scoreDoc in scoreDocs)
                {
                    all.Add(MapHit(searcher.Doc(scoreDoc.Doc)));
                }

                spotlight = FindSpotlight(all);
                results = all.Take(criteria.PageSize).ToList();
            }
            else
            {
                var offset = (long)criteria.Page * criteria.PageSize;
                results = offset >= scoreDocs.Length
                    ? []
                    : scoreDocs
                        .Skip((int)offset)
                        .Take(criteria.PageSize)
                        .Select(scoreDoc => MapHit(searcher.Doc(scoreDoc.Doc)))
                        .ToList();
            }

            return new RecipeSearchResults
            {
                Results = results,
                Total = outcome.Hits.TotalHits,
                Page = criteria.Page,
                PageSize = criteria.PageSize,
                CategoryFacets = ReadFacetCounts(outcome.Facets, RecipeSearchConstants.FacetCategory),
                DietFacets = ReadFacetCounts(outcome.Facets, RecipeSearchConstants.FacetDiet),
                Spotlight = spotlight,
            };
        });
    }

    private static FacetsConfig BuildFacetsConfig()
    {
        // Must mirror RecipeSearchIndexingStrategy.FacetsConfigFactory so the drill-down terms are encoded the
        // same way they were indexed; both dimensions were declared multi-valued there.
        var config = new FacetsConfig();
        config.SetMultiValued(RecipeSearchConstants.FacetCategory, true);
        config.SetMultiValued(RecipeSearchConstants.FacetDiet, true);
        return config;
    }

    private static Query BuildQuery(RecipeSearchCriteria criteria)
    {
        var textQuery = BuildTextQuery(criteria.Query);
        if (!criteria.TimeActive)
        {
            return textQuery;
        }

        var timeQuery = NumericRangeQuery.NewInt32Range(
            RecipeSearchConstants.FieldFastestTime, criteria.TimeMin, criteria.TimeMax, true, true);

        var combined = new BooleanQuery();
        combined.Add(textQuery, Occur.MUST);
        combined.Add(timeQuery, Occur.MUST);
        return combined;
    }

    private static Query BuildTextQuery(string queryText)
    {
        if (string.IsNullOrWhiteSpace(queryText))
        {
            return new MatchAllDocsQuery();
        }

        var terms = Tokenize(queryText);
        if (terms.Count == 0)
        {
            // The input was only punctuation or stop words; treat it as "everything", same as a blank query.
            return new MatchAllDocsQuery();
        }

        // Every term must match (AND), where a term may land in the boosted name or the content blob (OR per term).
        var query = new BooleanQuery();
        foreach (var term in terms)
        {
            var perTerm = new BooleanQuery();
            var nameClause = new TermQuery(new Term(RecipeSearchConstants.FieldName, term))
            {
                Boost = NameBoost,
            };
            perTerm.Add(nameClause, Occur.SHOULD);
            perTerm.Add(new TermQuery(new Term(RecipeSearchConstants.FieldContent, term)), Occur.SHOULD);
            query.Add(perTerm, Occur.MUST);
        }

        return query;
    }

    private static List<string> Tokenize(string text)
    {
        // Analyze with the same analyzer family used at index time so query terms match indexed terms; because we
        // build TermQuery values directly (never parsing query syntax) there is no query-injection surface.
        var terms = new List<string>();
        using var analyzer = new StandardAnalyzer(LuceneVersion.LUCENE_48);
        using var stream = analyzer.GetTokenStream(RecipeSearchConstants.FieldContent, text);
        var termAttribute = stream.AddAttribute<ICharTermAttribute>();
        stream.Reset();
        while (stream.IncrementToken())
        {
            var term = termAttribute.ToString();
            if (!string.IsNullOrEmpty(term))
            {
                terms.Add(term);
            }
        }

        stream.End();
        return terms;
    }

    private static Sort BuildSort(RecipeSearchCriteria criteria)
    {
        var (field, descending, byScore) = RecipeSearchCriteria.SortSpec(criteria.Sort);
        if (byScore)
        {
            return Sort.RELEVANCE;
        }

        var type = field switch
        {
            RecipeSearchConstants.FieldAverageRating => SortFieldType.DOUBLE,
            RecipeSearchConstants.FieldPublished => SortFieldType.INT64,
            RecipeSearchConstants.FieldVariantCount => SortFieldType.INT32,
            _ => SortFieldType.SCORE,
        };

        return type == SortFieldType.SCORE
            ? Sort.RELEVANCE
            : new Sort(new SortField(field, type, descending));
    }

    private static RecipeSearchHit MapHit(Document doc)
    {
        var reviewCount = ParseInt(doc.Get(RecipeSearchConstants.FieldReviewCount));

        double? averageRating = null;
        if (reviewCount > 0 &&
            double.TryParse(
                doc.Get(RecipeSearchConstants.FieldAverageRating + AverageRatingStoredSuffix),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out var parsedRating))
        {
            averageRating = parsedRating;
        }

        var tags = doc.Get(RecipeSearchConstants.FieldTags) ?? string.Empty;

        return new RecipeSearchHit
        {
            Name = doc.Get(RecipeSearchConstants.FieldName) ?? string.Empty,
            Slug = doc.Get(RecipeSearchConstants.FieldSlug) ?? string.Empty,
            Icon = doc.Get(RecipeSearchConstants.FieldIcon) ?? string.Empty,
            Category = doc.Get(RecipeSearchConstants.FieldCategory) ?? string.Empty,
            StartedBy = doc.Get(RecipeSearchConstants.FieldStartedBy) ?? string.Empty,
            Tags = tags.Split(';', StringSplitOptions.RemoveEmptyEntries),
            AverageRating = averageRating,
            ReviewCount = reviewCount,
            VariantCount = ParseInt(doc.Get(RecipeSearchConstants.FieldVariantCount)),
            FastestTime = ParseInt(doc.Get(RecipeSearchConstants.FieldFastestTime)),
        };
    }

    private static RecipeSearchHit FindSpotlight(IReadOnlyList<RecipeSearchHit> hits)
    {
        RecipeSearchHit best = null;
        var bestRating = double.NegativeInfinity;
        foreach (var hit in hits)
        {
            if (hit.ReviewCount > 0 && hit.AverageRating is double rating && rating > bestRating)
            {
                bestRating = rating;
                best = hit;
            }
        }

        return best;
    }

    private static IReadOnlyDictionary<string, int> ReadFacetCounts(Facets facets, string dimension)
    {
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        if (facets is null)
        {
            return counts;
        }

        // GetTopChildren returns null for a dimension with no matching documents.
        var result = facets.GetTopChildren(FacetTopN, dimension);
        if (result?.LabelValues is null)
        {
            return counts;
        }

        foreach (var labelValue in result.LabelValues)
        {
            counts[labelValue.Label] = (int)labelValue.Value;
        }

        return counts;
    }

    private static int ParseInt(string value) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : 0;
}
