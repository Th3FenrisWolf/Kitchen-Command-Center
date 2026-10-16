using System.Collections.Concurrent;
using KCC.E2ETests.Config;
using Microsoft.Playwright;

namespace KCC.E2ETests.Features.Members;

[NotInParallel(MemberSession.Serial)]
public class RecipeImportLeaveTests : BasePageTests
{
    // Safari and iOS focus neither a link nor a button on a click, so an open header card stays open behind the dialog.
    private const string NoFocusOnClick =
        "document.addEventListener('mousedown', (event) => { if (event.target.closest?.('a[href], button')) event.preventDefault() }, true)";

    private ILocator Logo => Page.Locator("#pad-bar-desk .pad-logo");

    private ILocator Cancel => Page.GetByRole(AriaRole.Link, new() { Name = "Cancel", Exact = true });

    private ILocator RecipesMenu => Page.Locator("#pad-menu-recipes");

    private ILocator LeaveDialog => Page.GetByTestId("leave-dialog");

    [Test]
    public async Task TheLogo_AsksBeforeLeavingCreateRecipe_WhileAPhotoIsHeld()
    {
        await PhotoImport.OpenOnCreateRecipeAsync(Page);
        await PhotoImport.AddFromDeviceAsync(Page, PhotoImport.FirstPage);

        await AsksBeforeFollowingAsync(Logo, "/");
    }

    [Test]
    public async Task TheHerosCancel_AsksBeforeLeavingAddVariant_WhileAPhotoIsHeld()
    {
        await PhotoImport.OpenOnAddVariantAsync(Page);
        await PhotoImport.AddFromDeviceAsync(Page, PhotoImport.FirstPage);

        await AsksBeforeFollowingAsync(Cancel, PhotoImport.RecipePath);
    }

    [Test]
    public async Task ALinkInTheRecipesCard_Asks_AndStayOrEscapeReturnsFocusToTheRecipesMenu()
    {
        await PhotoImport.OpenOnCreateRecipeAsync(Page);
        await PhotoImport.AddFromDeviceAsync(Page, PhotoImport.FirstPage);

        await FollowsALinkInTheRecipesCardAsync();
        await Page.GetByTestId("leave-stay").ClickAsync();
        await StaysWithTheFocusOnAsync(RecipesMenu);

        await FollowsALinkInTheRecipesCardAsync();
        await Page.Keyboard.PressAsync("Escape");
        await StaysWithTheFocusOnAsync(RecipesMenu);
    }

    [Test]
    public async Task Stay_ReturnsFocusToTheRecipesMenu_WhenAClickFocusesNothing()
    {
        await Page.AddInitScriptAsync(NoFocusOnClick);
        await PhotoImport.OpenOnCreateRecipeAsync(Page);
        await PhotoImport.AddFromDeviceAsync(Page, PhotoImport.FirstPage);

        await FollowsALinkInTheRecipesCardAsync();
        await Page.GetByTestId("leave-stay").ClickAsync();

        await StaysWithTheFocusOnAsync(RecipesMenu);
    }

    [Test]
    public async Task AReadThatSettlesWhileTheDialogIsOpen_LeavesTheFocusOnStay()
    {
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await Page.RouteAsync(
            "**/api/recipe-imports/read",
            async route =>
            {
                await release.Task;
                await route.FulfillAsync(new()
                {
                    Status = 409,
                    ContentType = "application/json",
                    Body = """{"error":"Photo import is resting.","code":"ceilingReached","availableAgainOn":"2026-11-01"}""",
                });
            });
        await PhotoImport.OpenOnCreateRecipeAsync(Page);
        await PhotoImport.AddFromDeviceAsync(Page, PhotoImport.FirstPage);
        await Page.GetByTestId("read-photos").ClickAsync();
        await Expect(Page.GetByTestId("reading-state")).ToBeVisibleAsync();

        await Logo.ClickAsync();
        await Expect(LeaveDialog).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("leave-stay")).ToBeFocusedAsync();

        release.SetResult();

        await Expect(Page.GetByTestId("import-unavailable")).ToBeAttachedAsync();
        await Expect(LeaveDialog).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("leave-stay")).ToBeFocusedAsync();
    }

    [Test]
    public async Task AReload_StillAsks_WhileAPhotoIsHeld()
    {
        await PhotoImport.OpenOnCreateRecipeAsync(Page);
        await PhotoImport.AddFromDeviceAsync(Page, PhotoImport.FirstPage);
        var dialogs = BrowserDialogs();

        _ = await Page.ReloadAsync();

        _ = await Assert.That(dialogs).IsEquivalentTo(["beforeunload"]);
    }

    [Test]
    public async Task TheLogo_LeavesCreateRecipeAtOnce_WithNoPhoto()
    {
        await PhotoImport.OpenOnCreateRecipeAsync(Page);

        await LeavesAtOnceAsync(Logo, "/");
    }

    [Test]
    public async Task TheHerosCancel_LeavesAddVariantAtOnce_WithNoPhoto()
    {
        await PhotoImport.OpenOnAddVariantAsync(Page);

        await LeavesAtOnceAsync(Cancel, PhotoImport.RecipePath);
    }

    private async Task AsksBeforeFollowingAsync(ILocator link, string destination)
    {
        var before = Page.Url;

        await link.ClickAsync();
        await Expect(LeaveDialog).ToBeVisibleAsync();
        _ = await Assert.That(Page.Url).IsEqualTo(before);
        await Expect(LeaveDialog).ToHaveAttributeAsync("role", "alertdialog");
        await Expect(LeaveDialog).ToHaveAttributeAsync("aria-modal", "true");
        await Expect(LeaveDialog.Locator(".kcc-blank")).ToHaveCountAsync(1);
        await Expect(Page.GetByTestId("leave-stay")).ToBeFocusedAsync();
        _ = await Assert.That(await FocusTrap.HoldsAsync(Page, LeaveDialog)).IsTrue();

        await Page.GetByTestId("leave-stay").ClickAsync();
        await StaysWithTheFocusOnAsync(link);

        await link.ClickAsync();
        await Expect(LeaveDialog).ToBeVisibleAsync();
        await Page.Keyboard.PressAsync("Escape");
        await StaysWithTheFocusOnAsync(link);

        await link.ClickAsync();
        var dialogs = BrowserDialogs();
        await Page.GetByTestId("leave-confirm").ClickAsync();
        await Page.WaitForURLAsync(url => new Uri(url).AbsolutePath == destination);
        _ = await Assert.That(dialogs).IsEmpty();
    }

    private async Task FollowsALinkInTheRecipesCardAsync()
    {
        await RecipesMenu.ClickAsync();
        await Page.Locator("#pad-card-recipes a").First.ClickAsync();
        await Expect(LeaveDialog).ToBeVisibleAsync();
    }

    private async Task StaysWithTheFocusOnAsync(ILocator element)
    {
        await Expect(LeaveDialog).ToHaveCountAsync(0);
        await Expect(Page.GetByTestId("photo-tile")).ToHaveCountAsync(1);
        await Expect(element).ToBeFocusedAsync();
    }

    private async Task LeavesAtOnceAsync(ILocator link, string destination)
    {
        var dialogs = BrowserDialogs();

        await link.ClickAsync();

        await Page.WaitForURLAsync(url => new Uri(url).AbsolutePath == destination);
        _ = await Assert.That(dialogs).IsEmpty();
    }

    // Accepted, so that a prompt the page should not have raised cannot hold the navigation up as well.
    private ConcurrentQueue<string> BrowserDialogs()
    {
        var types = new ConcurrentQueue<string>();
        Page.Dialog += (_, dialog) =>
        {
            types.Enqueue(dialog.Type);
            _ = dialog.AcceptAsync();
        };
        return types;
    }
}
