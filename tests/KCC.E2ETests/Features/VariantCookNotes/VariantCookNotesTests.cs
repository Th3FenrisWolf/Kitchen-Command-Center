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
        await MemberSession.SignInAsync(Page);
        _ = await RecipeNavigation.GoToFirstRecipeAsync(Page);
        _ = await RecipeNavigation.GoToFirstVariantAsync(Page);

        // The variant page renders two textareas (cook-note draft + review editor); the cook-note
        // draft and its Add button carry data-testid hooks so we target the right one regardless
        // of DOM order and independently of the (admin-seeded) "Add Cook Note" string.
        var noteText = $"E2E note {Guid.NewGuid():N}";
        await Page.Locator("[data-testid='cook-note-input']").FillAsync(noteText);
        await Page.Locator("[data-testid='add-cook-note']").ClickAsync();

        // Scope to the notes list: in development the MiniProfiler widget echoes the executed
        // INSERT (with the note text as a parameter) into a <code> block on the page, so a
        // page-wide GetByText(noteText) matches two elements and trips Playwright strict mode.
        await Expect(Page.Locator("[data-testid='cook-notes-list']").GetByText(noteText)).ToBeVisibleAsync();
    }
}
