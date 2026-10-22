using System.Net;
using System.Text;
using System.Text.Json;
using KCC.Web.Features.Bloom;
using KCC.Web.Features.Ssr;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace KCC.UnitTests.Features.Ssr;

public class VueSsrServiceTests
{
    private static readonly BloomInputs Home = new(2671381617, "/");

    [Test]
    public async Task RenderAsync_SameMarkup_CachesPreviewAndLiveSeparately()
    {
        var handler = new CountingHandler();
        var service = CreateService(handler);

        _ = await service.RenderAsync("<h/>", "<b/>", "<f/>", Home, isPreview: false);
        _ = await service.RenderAsync("<h/>", "<b/>", "<f/>", Home, isPreview: true);

        _ = await Assert.That(handler.Calls).IsEqualTo(2);
    }

    [Test]
    public async Task RenderAsync_RepeatedLiveRender_IsServedFromTheCache()
    {
        var handler = new CountingHandler();
        var service = CreateService(handler);

        _ = await service.RenderAsync("<h/>", "<b/>", "<f/>", Home, isPreview: false);
        _ = await service.RenderAsync("<h/>", "<b/>", "<f/>", Home, isPreview: false);

        _ = await Assert.That(handler.Calls).IsEqualTo(1);
    }

    [Test]
    public async Task RenderAsync_SendsTheBloomInputsWithTheRegions()
    {
        var handler = new CountingHandler();
        var service = CreateService(handler);

        _ = await service.RenderAsync("<h/>", "<b/>", "<f/>", new BloomInputs(4294967295, "/recipes/gnocchi"));

        using var body = JsonDocument.Parse(handler.LastBody);
        var bloom = body.RootElement.GetProperty("bloom");
        _ = await Assert.That(bloom.GetProperty("seed").GetUInt32()).IsEqualTo(4294967295u);
        _ = await Assert.That(bloom.GetProperty("page").GetString()).IsEqualTo("/recipes/gnocchi");
        _ = await Assert.That(body.RootElement.GetProperty("bodyContent").GetString()).IsEqualTo("<b/>");
    }

    [Test]
    public async Task RenderAsync_TwoSeeds_NeverShareACachedRender()
    {
        var handler = new CountingHandler();
        var service = CreateService(handler);

        _ = await service.RenderAsync("<h/>", "<b/>", "<f/>", new BloomInputs(1, "/"));
        _ = await service.RenderAsync("<h/>", "<b/>", "<f/>", new BloomInputs(2, "/"));

        _ = await Assert.That(handler.Calls).IsEqualTo(2);
    }

    [Test]
    public async Task RenderAsync_TwoPagesWithTheSameRegions_NeverShareACachedRender()
    {
        var handler = new CountingHandler();
        var service = CreateService(handler);

        _ = await service.RenderAsync("<h/>", "<b/>", "<f/>", new BloomInputs(1, "/recipes"));
        _ = await service.RenderAsync("<h/>", "<b/>", "<f/>", new BloomInputs(1, "/kitchens"));

        _ = await Assert.That(handler.Calls).IsEqualTo(2);
    }

    [Test]
    public async Task RenderAsync_CacheHit_ReturnsTheCachedScheduleWithTheMarkup()
    {
        var handler = new CountingHandler();
        var service = CreateService(handler);

        var first = await service.RenderAsync("<h/>", "<b/>", "<f/>", Home);
        var second = await service.RenderAsync("<h/>", "<b/>", "<f/>", Home);

        _ = await Assert.That(handler.Calls).IsEqualTo(1);
        _ = await Assert.That(first.Schedule).IsEqualTo(CountingHandler.Schedule);
        _ = await Assert.That(second.Schedule).IsEqualTo(CountingHandler.Schedule);
        _ = await Assert.That(second.Html).IsEqualTo("<div></div>");
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
        public const string Schedule = ".kcc-wash[data-wash=\"0a1b2c3d\"]{--wash-at:12.500s}";

        public int Calls { get; private set; }

        public string LastBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            LastBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            var response = JsonSerializer.Serialize(new { html = "<div></div>", schedule = Schedule, renderTime = 1 });
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(response, Encoding.UTF8, "application/json"),
            };
        }
    }
}
