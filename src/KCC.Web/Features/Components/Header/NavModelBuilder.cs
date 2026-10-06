using System.Globalization;
using KCC.Web.Features.Dictionary;
using KCC.Web.Features.Models.Generated;
using KCC.Web.Features.Pages.Account;
using KCC.Web.Features.Pages.Account.Login;
using KCC.Web.Features.Pages.Account.Logout;
using KCC.Web.Features.Pages.SurpriseMe;
using KCC.Web.Features.Recipes;
using KCC.Web.Features.Search;
using Microsoft.AspNetCore.Diagnostics;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;

namespace KCC.Web.Features.Components.Header;

public class NavModelBuilder(
    IUmbracoContextFactory umbracoContextFactory,
    IPublishedContentQuery contentQuery,
    IRecipeQueries recipes,
    AccountPageQueries accountPages,
    SiteSettingsQueries siteSettings,
    LibraryCountsCache libraryCounts,
    KitchenSummaries kitchenSummaries,
    IMemberManager memberManager,
    IMemberService memberService,
    IResourceStringProvider resourceStrings,
    LinkGenerator links)
{
    private static readonly IReadOnlyDictionary<string, string> NoIcons = new Dictionary<string, string>();

    private static readonly string[] MembersOnlyPages =
    [
        AccountPage.ModelTypeAlias,
        AccountSettingsPage.ModelTypeAlias,
        CreateRecipePage.ModelTypeAlias,
        AddVariantPage.ModelTypeAlias,
    ];

    public static NavModel Build(NavInput input)
    {
        var library = input.Urls.Library;
        var meals = Rows(input.Settings.Meals, input.Taxonomy.Categories, input.Counts.Categories, library, "category", input.CategoryIcons);

        return new NavModel
        {
            CurrentSection = SectionOf(input),
            RecipeTotal = input.Counts.Total,
            Recipes = new NavRecipes(
                meals,
                Rows(input.Settings.Diets, input.Taxonomy.Diets, input.Counts.Diets, library, "diet", NoIcons),
                QuickPicks(input),
                input.Settings.RecipesNote),
            Suggestions = input.Settings.SearchSuggestions.Count > 0
                ? input.Settings.SearchSuggestions
                : meals.OrderByDescending(meal => meal.Count).Take(3).Select(meal => meal.Label).ToList(),
            Member = input.Member,
            Urls = UrlsFor(input),
            Labels = input.Labels,
        };
    }

    public async Task<NavModel> BuildAsync(HttpContext context)
    {
        // Umbraco opens no context for a request whose path has a file extension, and the error page renders the header
        // for those too, while every page URL below is built from one. The exception handler also re-executes the
        // request at /error, where only its feature still holds the page the visitor asked for.
        using var umbraco = umbracoContextFactory.EnsureUmbracoContext();
        var signedIn = context.User.Identity?.IsAuthenticated == true ? await memberManager.GetCurrentMemberAsync() : null;
        var page = umbraco.UmbracoContext.PublishedRequest?.PublishedContent;
        var home = contentQuery.ContentAtRoot().OfType<HomePage>().FirstOrDefault();
        var library = home?.Children<RecipeListingPage>().FirstOrDefault();
        var account = accountPages.GetUrls();

        return Build(new NavInput
        {
            PathAndQuery = PathAndQueryOf(context),
            PageTypes = page?.AncestorsOrSelf().Select(node => node.ContentType.Alias).ToList() ?? [],
            Member = signedIn is null ? null : MemberOf(signedIn),
            Urls = new NavPageUrls(
                home?.Url() ?? "/",
                library?.Url(),
                library is null ? null : recipes.GetCreateRecipeUrl(library),
                account.Login,
                account.Account,
                account.Settings,
                links.GetPathByAction(nameof(LogoutController.Index), ControllerExtensions.GetControllerName<LogoutController>()),
                links.GetPathByAction(nameof(SurpriseMeController.Index), ControllerExtensions.GetControllerName<SurpriseMeController>())),
            Taxonomy = recipes.GetTaxonomy(),
            CategoryIcons = recipes.GetCategoryIcons(),
            Settings = siteSettings.GetNavSettings(),
            Counts = libraryCounts.Current(),
            Labels = resourceStrings.GetGroup("Nav"),
        });
    }

    private static string PathAndQueryOf(HttpContext context)
    {
        var originalPath = context.Features.Get<IExceptionHandlerPathFeature>()?.Path;
        return (originalPath is null ? context.Request.Path : new PathString(originalPath)) + context.Request.QueryString;
    }

    private NavMember MemberOf(MemberIdentityUser signedIn)
    {
        var member = memberService.GetById(signedIn.Key);
        var firstName = member?.GetValue<string>("firstName");

        return new NavMember(
            string.IsNullOrWhiteSpace(firstName) ? signedIn.UserName : firstName.Trim(),
            AccountViewModel.FormatMemberSince(member?.CreateDate),
            kitchenSummaries.For(signedIn.Key));
    }

    private static string SectionOf(NavInput input)
    {
        if (input.PageTypes.Contains(RecipeListingPage.ModelTypeAlias))
        {
            return NavSections.Recipes;
        }

        var page = input.PageTypes.FirstOrDefault();
        return input.Member is not null && (page is AccountPage.ModelTypeAlias or AccountSettingsPage.ModelTypeAlias)
            ? NavSections.Kitchen
            : null;
    }

    private static List<NavRow> Rows(
        IReadOnlyList<string> picks,
        IReadOnlyList<string> all,
        IReadOnlyDictionary<string, int> counts,
        string library,
        string filter,
        IReadOnlyDictionary<string, string> icons)
    {
        if (library is null)
        {
            return [];
        }

        var picked = picks.Where(pick => all.Contains(pick)).Distinct().ToList();
        return (picked.Count > 0 ? picked : all)
            .Where(name => counts.GetValueOrDefault(name) > 0)
            .Select(name => new NavRow(name, Filtered(library, filter, name), counts[name], icons.GetValueOrDefault(name)))
            .ToList();
    }

    private static List<NavRow> QuickPicks(NavInput input)
    {
        var picks = input.Settings.QuickPicks.Count > 0
            ? input.Settings.QuickPicks
            : NavPresets.All.Select(preset => new NavQuickPickSetting(preset, null, null, null)).ToList();

        return picks
            .Select(pick => pick.Preset is null ? QuickLink(pick) : Preset(pick, input))
            .Where(row => row is not null)
            .ToList();
    }

    private static NavRow QuickLink(NavQuickPickSetting pick) =>
        string.IsNullOrWhiteSpace(pick.Label) || string.IsNullOrWhiteSpace(pick.Url)
            ? null
            : new NavRow(pick.Label.Trim(), pick.Url, Target: string.IsNullOrWhiteSpace(pick.Target) ? null : pick.Target);

    private static NavRow Preset(NavQuickPickSetting pick, NavInput input)
    {
        var library = input.Urls.Library;
        var total = input.Counts.Total;
        (string LabelKey, string Url, string Icon, int? Count) preset = pick.Preset switch
        {
            NavPresets.UnderThirtyMinutes => (
                "Nav.UnderThirtyMinutes",
                Filtered(library, "timeMax", LibraryCounts.QuickTimeMax.ToString(CultureInfo.InvariantCulture)),
                "fa-duotone fa-stopwatch",
                input.Counts.UnderThirtyMinutes),
            NavPresets.TopRated => ("Nav.TopRated", Filtered(library, "sort", "rated"), "fa-duotone fa-star", null),
            NavPresets.MostVariants => ("Nav.MostVariants", Filtered(library, "sort", "variants"), "fa-duotone fa-layer-group", null),
            NavPresets.Newest => ("Nav.Newest", Filtered(library, "sort", "recent"), "fa-duotone fa-sparkles", null),
            NavPresets.SurpriseMe => ("Nav.SurpriseMe", input.Urls.SurpriseMe, "fa-duotone fa-dice", null),
            _ => default,
        };

        if (preset.Url is null || (preset.Count ?? total) == 0)
        {
            return null;
        }

        var label = string.IsNullOrWhiteSpace(pick.Label) ? input.Labels.GetValueOrDefault(preset.LabelKey, preset.LabelKey) : pick.Label.Trim();
        return new NavRow(label, preset.Url, preset.Count, preset.Icon);
    }

    private static string Filtered(string library, string name, string value) =>
        library is null ? null : $"{library}{QueryString.Create(name, value)}";

    private static NavUrls UrlsFor(NavInput input)
    {
        var urls = new NavUrls
        {
            Home = input.Urls.Home,
            Library = input.Urls.Library,
            SurpriseMe = input.Urls.SurpriseMe,
            CurrentPage = input.PathAndQuery,
        };

        var login = input.Urls.Login;
        if (input.Member is null)
        {
            return login is null
                ? urls
                : urls with
                {
                    SignIn = SignInRedirect.UrlFor(login, input.PathAndQuery),
                    Register = $"{login}?mode={LoginPageController.RegisterMode}",
                };
        }

        var returnTo = MembersOnlyPages.Contains(input.PageTypes.FirstOrDefault()) ? input.Urls.Home : input.PathAndQuery;
        return urls with
        {
            NewRecipe = input.Urls.CreateRecipe,
            Account = input.Urls.Account,
            Settings = input.Urls.Settings,
            SignOut = $"{input.Urls.Logout}?returnUrl={Uri.EscapeDataString(returnTo)}",
        };
    }
}

public sealed record NavInput
{
    public string PathAndQuery { get; init; }

    public IReadOnlyList<string> PageTypes { get; init; } = [];

    public NavMember Member { get; init; }

    public NavPageUrls Urls { get; init; }

    public RecipeTaxonomy Taxonomy { get; init; }

    public IReadOnlyDictionary<string, string> CategoryIcons { get; init; } = new Dictionary<string, string>();

    public NavSettings Settings { get; init; } = NavSettings.Empty;

    public LibraryCounts Counts { get; init; }

    public IReadOnlyDictionary<string, string> Labels { get; init; } = new Dictionary<string, string>();
}

public sealed record NavPageUrls(
    string Home,
    string Library,
    string CreateRecipe,
    string Login,
    string Account,
    string Settings,
    string Logout,
    string SurpriseMe);
