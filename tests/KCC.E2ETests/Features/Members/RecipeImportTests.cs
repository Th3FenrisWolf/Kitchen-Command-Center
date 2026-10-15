using KCC.E2ETests.Config;
using Microsoft.Playwright;

namespace KCC.E2ETests.Features.Members;

[NotInParallel(MemberSession.Serial)]
public class RecipeImportTests : BasePageTests
{
    private const string WakeLockRecorder =
        """
        window.wakeLocks = { requested: [], released: 0 }
        Object.defineProperty(navigator, 'wakeLock', {
          configurable: true,
          value: {
            request: async (type) => {
              window.wakeLocks.requested.push(type)
              return { release: async () => { window.wakeLocks.released += 1 } }
            },
          },
        })
        """;

    private const string PhotoHold =
        """
        (() => {
          let gate = null
          let open = () => {}
          window.photoHold = {
            hold: () => { gate = new Promise((resolve) => { open = resolve }) },
            release: () => {
              gate = null
              open()
            },
          }
          const held = () => gate ?? Promise.resolve()
          const decode = window.createImageBitmap.bind(window)
          window.createImageBitmap = async (...args) => {
            await held()
            return decode(...args)
          }
          if (window.ImageCapture) {
            const takePhoto = ImageCapture.prototype.takePhoto
            ImageCapture.prototype.takePhoto = async function (...args) {
              await held()
              return takePhoto.apply(this, args)
            }
          }
        })()
        """;

    [Test]
    public async Task ARecipeReadFromPhotos_ReachesAMarkedReview_AndWaitsOnTheAccountPage()
    {
        await PhotoImport.OpenOnCreateRecipeAsync(Page);
        await PhotoImport.AddFromDeviceAsync(Page, PhotoImport.FirstPage, PhotoImport.SecondPage);
        await Page.GetByTestId("photo-remove").Nth(1).ClickAsync();
        await Expect(Page.GetByTestId("photo-tile")).ToHaveCountAsync(1);

        await Page.GetByTestId("read-photos").ClickAsync();

        await Expect(Page.GetByTestId("review-step")).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("match-step")).ToHaveCountAsync(0);
        await Expect(Mark("estimate").First).ToBeVisibleAsync();
        await Expect(Mark("unclear").First).ToBeVisibleAsync();

        await ViewsAPhoto_AndReturnsFocusToItsTileAsync();

        await Page.GetByTestId("wizard-submit").ClickAsync();
        await Expect(Page.GetByTestId("wizard-submitted")).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("wizard-closing-link")).ToHaveAttributeAsync("href", "/recipes/");

        _ = await Page.GotoAsync("/account");
        var group = Page.Locator("li").Filter(new() { HasText = PhotoImport.StandInRecipeName });
        await Expect(group.GetByText("Pending review").First).ToBeVisibleAsync();
    }

    [Test]
    public async Task AVariantReadFromAPhoto_LeadsBackToItsRecipe()
    {
        await PhotoImport.OpenOnAddVariantAsync(Page);
        await PhotoImport.AddFromDeviceAsync(Page, PhotoImport.FirstPage);

        await Page.GetByTestId("read-photos").ClickAsync();

        await Expect(Page.GetByTestId("review-step")).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("match-step")).ToHaveCountAsync(0);
        await Page.GetByTestId("wizard-submit").ClickAsync();
        var closing = Page.GetByTestId("wizard-closing-link");
        await Expect(closing).ToContainTextAsync(PhotoImport.RecipeName);
        await Expect(closing).ToHaveAttributeAsync("href", PhotoImport.RecipePath);
    }

    [Test]
    public async Task ARead_HoldsOneScreenWakeLock_UntilTheReviewOpens()
    {
        await Page.AddInitScriptAsync(WakeLockRecorder);
        await PhotoImport.OpenOnCreateRecipeAsync(Page);
        await PhotoImport.AddFromDeviceAsync(Page, PhotoImport.FirstPage);

        await Page.GetByTestId("read-photos").ClickAsync();

        await Expect(Page.GetByTestId("review-step")).ToBeVisibleAsync();
        _ = await Page.WaitForFunctionAsync("() => window.wakeLocks.released > 0");
        var locks = await Page.EvaluateAsync<WakeLocks>("() => window.wakeLocks");
        _ = await Assert.That(locks.Requested).IsEquivalentTo(["screen"]);
        _ = await Assert.That(locks.Released).IsEqualTo(1);
    }

    [Test]
    public async Task ReadRecipe_StaysOff_WhileAPhotoIsStillBeingTakenOrEncoded()
    {
        await Page.AddInitScriptAsync(PhotoHold);
        await PhotoImport.OpenOnCreateRecipeAsync(Page);
        await PhotoImport.AddFromDeviceAsync(Page, PhotoImport.FirstPage);
        var read = Page.GetByTestId("read-photos");
        var tiles = Page.GetByTestId("photo-tile");
        await Expect(read).ToBeEnabledAsync();

        await Page.EvaluateAsync("() => window.photoHold.hold()");
        await Page.GetByTestId("photo-input").SetInputFilesAsync(PhotoImport.SecondPage);
        await Expect(read).ToBeDisabledAsync();
        await Expect(tiles).ToHaveCountAsync(1);
        await Page.EvaluateAsync("() => window.photoHold.release()");
        await Expect(tiles).ToHaveCountAsync(2);
        await Expect(read).ToBeEnabledAsync();

        await Expect(Page.GetByTestId("camera-view")).ToBeVisibleAsync();
        await Page.EvaluateAsync("() => window.photoHold.hold()");
        await Page.GetByTestId("camera-shutter").ClickAsync();
        await Expect(read).ToBeDisabledAsync();
        await Expect(tiles).ToHaveCountAsync(2);
        await Page.EvaluateAsync("() => window.photoHold.release()");
        await Expect(tiles).ToHaveCountAsync(3);
        await Expect(read).ToBeEnabledAsync();
    }

    [Test]
    public async Task AShotStillBeingTaken_IsDropped_WhenTheMemberGoesBack()
    {
        await Page.AddInitScriptAsync(PhotoHold);
        await PhotoImport.OpenOnCreateRecipeAsync(Page);
        await PhotoImport.AddFromDeviceAsync(Page, PhotoImport.FirstPage);
        var read = Page.GetByTestId("read-photos");
        await Expect(Page.GetByTestId("camera-view")).ToBeVisibleAsync();
        await Page.EvaluateAsync("() => window.photoHold.hold()");
        await Page.GetByTestId("camera-shutter").ClickAsync();
        await Expect(read).ToBeDisabledAsync();

        await Page.GetByRole(AriaRole.Button, new() { Name = "Back", Exact = true }).ClickAsync();
        await Page.GetByTestId("wizard-start-photos").ClickAsync();

        await Expect(read).ToBeEnabledAsync();
        await Expect(Page.GetByTestId("photo-tile")).ToHaveCountAsync(1);
    }

    [Test]
    public async Task AReadThatMeetsTheCeiling_RestsTheImport_AndLetsTheMemberTypeItIn()
    {
        await Page.RouteAsync(
            "**/api/recipe-imports/read",
            route => route.FulfillAsync(new()
            {
                Status = 409,
                ContentType = "application/json",
                Body = """{"error":"Photo import is resting.","code":"ceilingReached","availableAgainOn":"2026-11-01"}""",
            }));
        await PhotoImport.OpenOnCreateRecipeAsync(Page);
        await PhotoImport.AddFromDeviceAsync(Page, PhotoImport.FirstPage);

        await Page.GetByTestId("read-photos").ClickAsync();

        await Expect(Page.GetByTestId("import-unavailable")).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("import-unavailable")).ToContainTextAsync("November 1, 2026");
        await Expect(Page.GetByTestId("photo-tile")).ToHaveCountAsync(1);
        await Expect(Page.GetByTestId("camera-view")).ToHaveCountAsync(0);
        await Expect(Page.GetByTestId("photo-input")).ToHaveCountAsync(0);
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Add photos", Exact = true })).ToHaveCountAsync(0);
        await Expect(Page.GetByTestId("read-photos")).ToBeDisabledAsync();

        await Page.GetByRole(AriaRole.Button, new() { Name = "Type it in", Exact = true }).ClickAsync();

        await Expect(Page.GetByTestId("wizard-recipe-name")).ToBeVisibleAsync();
    }

    private ILocator Mark(string kind) => Page.Locator($"[data-testid='value-mark'][data-kind='{kind}']");

    private async Task ViewsAPhoto_AndReturnsFocusToItsTileAsync()
    {
        var tile = Page.GetByTestId("wizard-photos").GetByTestId("photo-tile").First;
        var viewer = Page.GetByTestId("photo-viewer");

        await tile.ClickAsync();
        await Expect(viewer).ToHaveAttributeAsync("role", "dialog");
        await Expect(viewer).ToHaveAttributeAsync("aria-modal", "true");
        _ = await Assert.That(await FocusTrap.HoldsAsync(Page, viewer)).IsTrue();
        await viewer.GetByRole(AriaRole.Button, new() { Name = "Close", Exact = true }).ClickAsync();
        await Expect(viewer).ToHaveCountAsync(0);
        await Expect(tile).ToBeFocusedAsync();

        await tile.ClickAsync();
        await Expect(viewer).ToBeVisibleAsync();
        await Page.Keyboard.PressAsync("Escape");
        await Expect(viewer).ToHaveCountAsync(0);
        await Expect(tile).ToBeFocusedAsync();
    }

    private sealed class WakeLocks
    {
        public string[] Requested { get; set; } = [];

        public int Released { get; set; }
    }
}
