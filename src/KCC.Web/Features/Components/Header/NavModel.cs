using KCC.Web.Features.Pages.Account;

namespace KCC.Web.Features.Components.Header;

public sealed record NavModel
{
    public string CurrentSection { get; init; }

    public int RecipeTotal { get; init; }

    public NavRecipes Recipes { get; init; }

    public IReadOnlyList<string> Suggestions { get; init; } = [];

    public NavMember Member { get; init; }

    public NavUrls Urls { get; init; }

    public IReadOnlyDictionary<string, string> Labels { get; init; } = new Dictionary<string, string>();
}

public sealed record NavRecipes(IReadOnlyList<NavRow> Meals, IReadOnlyList<NavRow> Diets, IReadOnlyList<NavRow> QuickPicks, string Note);

public sealed record NavRow(string Label, string Url, int? Count = null, string Icon = null, string Target = null);

public sealed record NavMember(string FirstName, string MemberSince, KitchenSummary Kitchen);

public sealed record NavUrls
{
    public string Home { get; init; }

    public string Library { get; init; }

    public string SurpriseMe { get; init; }

    public string CurrentPage { get; init; }

    public string SignIn { get; init; }

    public string Register { get; init; }

    public string NewRecipe { get; init; }

    public string Account { get; init; }

    public string Settings { get; init; }

    public string SignOut { get; init; }
}
