using KCC.E2ETests.Config;
using Microsoft.Playwright;

namespace KCC.E2ETests.Features.VariantCookNotes;

[NotInParallel(MemberSession.Serial)]
public class VariantCookNotesTests : BasePageTests
{
    [Test]
    public async Task LoggedInMember_CanAddAndDeleteACookNote()
    {
        await MemberSession.SignInAsync(Page);
        _ = await Page.GotoAsync(MemberTestVariant.Path);
        var notes = Page.Locator("[data-testid='cook-notes-list']");
        var noteText = $"E2E note {Guid.NewGuid():N}";

        await Page.Locator("[data-testid='cook-note-input']").FillAsync(noteText);
        await Page.Locator("[data-testid='add-cook-note']").ClickAsync();
        await Expect(notes.GetByText(noteText)).ToBeVisibleAsync();

        await notes.Locator("li").Filter(new() { HasText = noteText }).GetByRole(AriaRole.Button).ClickAsync();
        await Expect(Page.GetByText(noteText)).ToHaveCountAsync(0);
    }
}
