using Microsoft.Playwright;

namespace KCC.E2ETests.Features.Chrome;

public class NotFoundTests : BasePageTests
{
    [Test]
    public async Task UnknownUrl_ShowsThe404Page()
    {
        var response = await Page.GotoAsync("/this-page-does-not-exist");

        _ = await Assert.That(response!.Status).IsEqualTo(404);
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Level = 1 })).ToContainTextAsync("find that page");
    }
}
