using Anthropic;
using Anthropic.Core;
using KCC.Admin;
using KCC.Web.Features.Models.Options;
using KCC.Web.Features.Providers;
using Microsoft.Extensions.Logging.Abstractions;

namespace KCC.UnitTests.Features.Providers;

public class RecipeIconProviderTests
{
    [Test]
    public async Task WithoutAnApiKey_PicksTheFallbackIconWithoutCallingAnthropic()
    {
        var handler = new CountingHandler();
        var provider = new RecipeIconProvider(
            new AnthropicClient(new ClientOptions { ApiKey = string.Empty, HttpClient = new HttpClient(handler) }),
            new AnthropicOptions { ApiKey = string.Empty },
            NullLogger<RecipeIconProvider>.Instance);

        var icon = await provider.PickAsync("Shakshuka", "Eggs in tomato.", ["Eggs"], CancellationToken.None);

        _ = await Assert.That(icon).IsEqualTo(RecipeIcons.Fallback("Shakshuka"));
        _ = await Assert.That(handler.Calls).IsEqualTo(0);
    }

    private sealed class CountingHandler : HttpMessageHandler
    {
        public int Calls { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromException<HttpResponseMessage>(new HttpRequestException("The recording handler refuses every request."));
        }
    }
}
