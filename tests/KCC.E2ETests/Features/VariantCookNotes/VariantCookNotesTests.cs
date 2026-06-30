using KCC.E2ETests.Config;
using Microsoft.Playwright;

namespace KCC.E2ETests.Features.VariantCookNotes;

// TUnit runs tests in parallel by default; concurrent navigations starve the single-threaded
// Vite dev server and every nav times out. [NotInParallel] serializes them (matches the
// AdminHomePageMiddlewareTests / CookModeTests precedent in this repo).
[NotInParallel]
public class VariantCookNotesTests : BasePageTests
{
    [Test]
    public async Task LoggedInMember_CanAddACookNote()
    {
        if (!MemberSession.HasCredentials)
        {
            Skip.Test("Set KCC_E2E_MEMBER_USERNAME/PASSWORD to a seeded confirmed member to run this flow.");
            return;
        }

        await MemberSession.SignInAsync(Page);
        _ = await RecipeNavigation.GoToFirstRecipeAsync(Page);
        _ = await RecipeNavigation.GoToFirstVariantAsync(Page);

        // The variant page renders two textareas (cook-note draft + review editor); the cook-note
        // draft and its Add button carry data-testid hooks so we target the right one regardless
        // of DOM order and independently of the (admin-seeded) "Add Cook Note" string.
        var noteText = $"E2E note {Guid.NewGuid():N}";
        await Page.Locator("[data-testid='cook-note-input']").FillAsync(noteText);
        await Page.Locator("[data-testid='add-cook-note']").ClickAsync();

        await Expect(Page.GetByText(noteText)).ToBeVisibleAsync();
    }
}
