using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Facet;
using Lucene.Net.Facet.Taxonomy.Directory;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;

namespace KCC.Web.Features.Search;

public static class RecipeIndexBuilder
{
    public static RecipeIndexSnapshot Build(IEnumerable<RecipeSearchDocument> documents)
    {
        var indexDirectory = new RAMDirectory();
        var taxonomyDirectory = new RAMDirectory();
        var facets = RecipeFacets.Config();

        using (var analyzer = new StandardAnalyzer(LuceneVersion.LUCENE_48))
        using (var writer = new IndexWriter(indexDirectory, new IndexWriterConfig(LuceneVersion.LUCENE_48, analyzer)))
        using (var taxonomyWriter = new DirectoryTaxonomyWriter(taxonomyDirectory))
        {
            foreach (var document in documents)
            {
                writer.AddDocument(facets.Build(taxonomyWriter, BuildDocument(document)));
            }

            // Committed even when empty: a reader can only open a directory that holds a commit.
            taxonomyWriter.Commit();
            writer.Commit();
        }

        return new RecipeIndexSnapshot(indexDirectory, taxonomyDirectory);
    }

    internal static Document BuildDocument(RecipeSearchDocument d)
    {
        var doc = new Document
        {
            new TextField(RecipeSearchConstants.FieldName, d.Name, Field.Store.YES),
            new SortedDocValuesField(RecipeSearchConstants.FieldNameSort, new BytesRef((d.Name ?? string.Empty).ToLowerInvariant())),
            new TextField(RecipeSearchConstants.FieldContent, RecipeSearchDocument.BuildContent(d), Field.Store.NO),
            new StringField(RecipeSearchConstants.FieldSlug, d.Slug, Field.Store.YES),
            new StringField(RecipeSearchConstants.FieldIcon, d.Icon, Field.Store.YES),
            new StringField(RecipeSearchConstants.FieldCategory, d.Category, Field.Store.YES),
            new StringField(RecipeSearchConstants.FieldStartedBy, d.StartedBy, Field.Store.YES),
            new StringField(RecipeSearchConstants.FieldTags, RecipeSearchDocument.JoinTags(d.Tags), Field.Store.YES),
            new Int32Field(RecipeSearchConstants.FieldFastestTime, d.FastestTime, Field.Store.YES),
            new Int32Field(RecipeSearchConstants.FieldVariantCount, d.VariantCount, Field.Store.YES),
            new Int32Field(RecipeSearchConstants.FieldReviewCount, d.ReviewCount, Field.Store.YES),
            new StoredField(RecipeSearchConstants.FieldAverageRating + "_v", d.AverageRating),
            new Int64Field(RecipeSearchConstants.FieldPublished, d.PublishedUnixSeconds, Field.Store.YES),
            new NumericDocValuesField(RecipeSearchConstants.FieldFastestTime, d.FastestTime),
            new NumericDocValuesField(RecipeSearchConstants.FieldVariantCount, d.VariantCount),
            new DoubleDocValuesField(RecipeSearchConstants.FieldAverageRating, d.AverageRating),
            new NumericDocValuesField(RecipeSearchConstants.FieldPublished, d.PublishedUnixSeconds),
        };

        if (!string.IsNullOrWhiteSpace(d.Category))
        {
            doc.Add(new FacetField(RecipeSearchConstants.FacetCategory, d.Category));
        }

        foreach (var diet in d.Diets.Where(tag => !string.IsNullOrWhiteSpace(tag)))
        {
            doc.Add(new FacetField(RecipeSearchConstants.FacetDiet, diet));
        }

        foreach (var style in d.Styles.Where(tag => !string.IsNullOrWhiteSpace(tag)))
        {
            doc.Add(new FacetField(RecipeSearchConstants.FacetStyle, style));
        }

        return doc;
    }
}
