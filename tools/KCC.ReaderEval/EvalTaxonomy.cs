using System.Text.Json;
using KCC.Web.Features.RecipeImport;
using KCC.Web.Features.Recipes;

namespace KCC.ReaderEval;

public static class EvalTaxonomy
{
    private const string TagAlias = "kind";
    private const string CategoryAlias = "icon";

    public static ReaderTaxonomy Create() => ReaderTaxonomy.From(Choices(), ImportFlows.Recipe);

    private static DetailsChoices Choices() => new(
        [.. TaxonomyDefaults.Values.Where(value => value.Alias == CategoryAlias).Select(value => new CategoryChoice(value.Key, value.Name))],
        [.. TaxonomyDefaults.Values.Where(value => value.Alias == TagAlias).Select(value => new TagChoice(value.Key, value.Name, Group(value)))]);

    private static string Group(TaxonomyDefault value) => JsonSerializer.Deserialize<string[]>(value.Stored)[0];
}
