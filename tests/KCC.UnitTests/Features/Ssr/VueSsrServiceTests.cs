using System.Net;
using System.Text;
using KCC.Web.Features.Ssr;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace KCC.UnitTests.Features.Ssr;

public class VueSsrServiceTests
{
    [Test]
    public async Task RenderAsync_SameMarkup_CachesPreviewAndLiveSeparately()
    {
        var handler = new CountingHandler();
        var service = CreateService(handler);

        _ = await service.RenderAsync("<h/>", "<b/>", "<f/>", isPreview: false);
        _ = await service.RenderAsync("<h/>", "<b/>", "<f/>", isPreview: true);

        _ = await Assert.That(handler.Calls).IsEqualTo(2);
    }

    [Test]
    public async Task RenderAsync_RepeatedLiveRender_IsServedFromTheCache()
    {
        var handler = new CountingHandler();
        var service = CreateService(handler);

        _ = await service.RenderAsync("<h/>", "<b/>", "<f/>", isPreview: false);
        _ = await service.RenderAsync("<h/>", "<b/>", "<f/>", isPreview: false);

        _ = await Assert.That(handler.Calls).IsEqualTo(1);
    }

    private static VueSsrService CreateService(HttpMessageHandler handler)
    {
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient("VueSsr")).Returns(() => new HttpClient(handler, disposeHandler: false) { BaseAddress = new Uri("http://ssr.test") });

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string> { ["VueSsr:Enabled"] = "true" })
            .Build();

        var environment = new Mock<IHostEnvironment>();
        environment.SetupGet(e => e.EnvironmentName).Returns("Testing");

        return new VueSsrService(
            factory.Object,
            new MemoryCache(new MemoryCacheOptions()),
            configuration,
            environment.Object,
            NullLogger<VueSsrService>.Instance);
    }

    private sealed class CountingHandler : HttpMessageHandler
    {
        public int Calls { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"html\":\"<div></div>\",\"renderTime\":1}", Encoding.UTF8, "application/json"),
            });
        }
    }
}
