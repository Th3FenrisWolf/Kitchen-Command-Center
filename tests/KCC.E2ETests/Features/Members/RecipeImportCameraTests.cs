using KCC.E2ETests.Config;
using Microsoft.Playwright;

namespace KCC.E2ETests.Features.Members;

[NotInParallel(MemberSession.Serial)]
public class RecipeImportCameraTests : BasePageTests
{
    [Test]
    public async Task TheCamera_ShowsItsPicture_AndEachShutterPressAddsAPhoto()
    {
        await PhotoImport.OpenOnCreateRecipeAsync(Page);
        var shutter = Page.GetByTestId("camera-shutter");
        var tiles = Page.GetByTestId("photo-tile");

        await Expect(Page.GetByTestId("camera-view")).ToBeVisibleAsync();
        await shutter.ClickAsync();
        await Expect(tiles).ToHaveCountAsync(1);
        await shutter.ClickAsync();

        await Expect(tiles).ToHaveCountAsync(2);
    }

    [Test]
    public async Task ABlockedCamera_SaysSo_AndPhotosStillComeFromTheDevice()
    {
        await Page.AddInitScriptAsync(CameraFailsWith("NotAllowedError"));
        await PhotoImport.OpenOnCreateRecipeAsync(Page);

        await Expect(Page.GetByTestId("camera-message"))
            .ToContainTextAsync("The camera is blocked. Allow it in your browser settings, or add photos from your device.");
        await PhotoImport.AddFromDeviceAsync(Page, PhotoImport.FirstPage);
    }

    [Test]
    public async Task AMissingCamera_SaysSo()
    {
        await Page.AddInitScriptAsync(CameraFailsWith("NotFoundError"));
        await PhotoImport.OpenOnCreateRecipeAsync(Page);

        await Expect(Page.GetByTestId("camera-message")).ToContainTextAsync("No camera found. Add photos from your device instead.");
    }

    [Test]
    public async Task TheCamera_StopsEveryTrack_OnceTheMemberLeavesThePhotoStep()
    {
        await Page.AddInitScriptAsync(CameraRecorder(holdBack: false));
        await PhotoImport.OpenOnCreateRecipeAsync(Page);
        var shutter = Page.GetByTestId("camera-shutter");
        var tiles = Page.GetByTestId("photo-tile");

        await shutter.ClickAsync();
        await Expect(tiles).ToHaveCountAsync(1);
        await shutter.ClickAsync();
        await Expect(tiles).ToHaveCountAsync(2);
        await Page.GetByTestId("read-photos").ClickAsync();
        await Expect(Page.GetByTestId("match-step")).ToBeVisibleAsync();

        await EveryCameraTrackHasEndedAsync();
    }

    [Test]
    public async Task ACameraStreamThatArrivesAfterThePhotoStepWasLeft_IsStoppedAtOnce()
    {
        await Page.AddInitScriptAsync(CameraRecorder(holdBack: true));
        await PhotoImport.OpenOnCreateRecipeAsync(Page);
        _ = await Page.WaitForFunctionAsync("() => window.camera.holding");

        await Page.GetByRole(AriaRole.Button, new() { Name = "Back", Exact = true }).ClickAsync();
        await Expect(Page.GetByTestId("wizard-start-photos")).ToBeVisibleAsync();
        await Page.EvaluateAsync("() => window.camera.release()");

        await EveryCameraTrackHasEndedAsync();
    }

    // The fake-UI launch flag grants the camera whatever the context's permissions say, so only a stub can refuse it.
    private static string CameraFailsWith(string name) =>
        $"navigator.mediaDevices.getUserMedia = () => Promise.reject(new DOMException('No camera in this test.', '{name}'))";

    // release() resolves a task later, so the page has handled the held stream before the test reads its tracks.
    private static string CameraRecorder(bool holdBack) =>
        $$"""
        (() => {
          const holdBack = {{(holdBack ? "true" : "false")}}
          const open = navigator.mediaDevices.getUserMedia.bind(navigator.mediaDevices)
          let letGo
          const held = new Promise((resolve) => { letGo = resolve })
          window.camera = {
            tracks: [],
            holding: false,
            release: () => {
              letGo()
              return new Promise((settled) => setTimeout(settled))
            },
          }
          navigator.mediaDevices.getUserMedia = async (constraints) => {
            const stream = await open(constraints)
            window.camera.tracks.push(...stream.getTracks())
            if (holdBack) {
              window.camera.holding = true
              await held
            }
            return stream
          }
        })()
        """;

    private async Task EveryCameraTrackHasEndedAsync()
    {
        var states = await Page.EvaluateAsync<string[]>("() => window.camera.tracks.map((track) => track.readyState)");

        _ = await Assert.That(states).IsNotEmpty();
        _ = await Assert.That(states).DoesNotContain("live");
    }
}
