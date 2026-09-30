using KCC.Web.Features.Pages.Error;

namespace KCC.UnitTests.Features.Pages.Error;

public class ErrorViewModelTests
{
    [Test]
    public async Task For_WithContent_UsesIt()
    {
        var viewModel = ErrorViewModel.For(404, "We couldn't find that page", "<p>Gone.</p>");

        _ = await Assert.That(viewModel.StatusCode).IsEqualTo(404);
        _ = await Assert.That(viewModel.Heading).IsEqualTo("We couldn't find that page");
        _ = await Assert.That(viewModel.Title).IsEqualTo("We couldn't find that page");
        _ = await Assert.That(viewModel.Body).IsEqualTo("<p>Gone.</p>");
    }

    [Test]
    public async Task For_WithoutContent_FallsBackToGenericText()
    {
        var viewModel = ErrorViewModel.For(500, null, null);

        _ = await Assert.That(viewModel.Heading).IsEqualTo("Error");
        _ = await Assert.That(viewModel.Title).IsEqualTo("Error");
        _ = await Assert.That(viewModel.Body).IsEqualTo("An unexpected error occurred.");
    }
}
