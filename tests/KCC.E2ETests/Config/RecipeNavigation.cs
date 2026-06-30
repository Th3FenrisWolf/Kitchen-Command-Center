using Microsoft.Playwright;

namespace KCC.E2ETests.Config;

/// <summary>Navigates the live site to a real recipe / variant page without hard-coding slugs.</summary>
public static class RecipeNavigation
{
    /// <summary>Opens the recipe listing and clicks into the first recipe; returns its URL.</summary>
    public static async Task<string> GoToFirstRecipeAsync(IPage page)
    {
        // The dev page holds an open Vite HMR WebSocket, so the default `load` event never settles
        // and GotoAsync would time out. The listing is server-rendered, so DOMContentLoaded already
        // exposes the recipe cards these helpers click into.
        _ = await page.GotoAsync("/recipes", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

        // Recipe cards render inside <main> as anchors to /recipes/<slug>. The listing also
        // surfaces a "Create Recipe" call-to-action pointing at /recipes/create-recipe, so we
        // exclude that link and take the first real recipe card. (The site nav/footer links live
        // outside <main>, so scoping to main already drops them.)
        var firstRecipe = page.Locator("main a[href*='/recipes/']:not([href*='create-recipe'])").First;
        await firstRecipe.ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        return page.Url;
    }

    /// <summary>From a recipe page, clicks into the first variant card; returns its URL.</summary>
    public static async Task<string> GoToFirstVariantAsync(IPage page)
    {
        // Variant cards expose a stable [data-variant-name] hook (VariantGrid/VariantList), which
        // disambiguates them from breadcrumbs and the "Add Variant" link that also match /recipes/.
        var firstVariant = page.Locator("[data-variant-name]").First;
        await firstVariant.ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        return page.Url;
    }
}
