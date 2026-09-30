using Lucene.Net.Facet.Taxonomy;
using Lucene.Net.Facet.Taxonomy.Directory;
using Lucene.Net.Index;
using Lucene.Net.Search;
using LuceneDirectory = Lucene.Net.Store.Directory;

namespace KCC.Web.Features.Search;

public sealed class RecipeIndexSnapshot : IDisposable
{
    private readonly LuceneDirectory indexDirectory;
    private readonly LuceneDirectory taxonomyDirectory;
    private readonly DirectoryReader reader;

    public RecipeIndexSnapshot(LuceneDirectory indexDirectory, LuceneDirectory taxonomyDirectory)
    {
        this.indexDirectory = indexDirectory;
        this.taxonomyDirectory = taxonomyDirectory;
        reader = DirectoryReader.Open(indexDirectory);
        Taxonomy = new DirectoryTaxonomyReader(taxonomyDirectory);
        Searcher = new IndexSearcher(reader);
    }

    public IndexSearcher Searcher { get; }

    public TaxonomyReader Taxonomy { get; }

    public void Dispose()
    {
        Taxonomy.Dispose();
        reader.Dispose();
        taxonomyDirectory.Dispose();
        indexDirectory.Dispose();
    }
}
