namespace KCC.E2ETests.Features.HomePage;

public class HomePageTests : BasePageTests
{
    [Test]
    public async Task Homepage_LoadsSuccessfully()
    {
        var response = await Page.GotoAsync("/");

        _ = await Assert.That(response).IsNotNull();
        _ = await Assert.That(response!.Ok).IsTrue();

        await Expect(Page.Locator("body")).ToBeVisibleAsync();
    }
}
