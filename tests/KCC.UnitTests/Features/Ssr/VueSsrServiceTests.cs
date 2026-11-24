using System.Net;
using System.Text;
using System.Text.Json;
using KCC.Web.Features.Bloom;
using KCC.Web.Features.Ssr;
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

    [Test]
    public async Task RenderAsync_AnEntryLargerThanTheCacheLimit_IsNeverStored()
    {
        var handler = new CountingHandler();
        var service = CreateService(handler, cacheLimit: 8);

        _ = await service.RenderAsync("<h/>", "<b/>", "<f/>", Home);
        _ = await service.RenderAsync("<h/>", "<b/>", "<f/>", Home);

        _ = await Assert.That(handler.Calls).IsEqualTo(2);
    }

    [Test]
    public async Task RenderAsync_CarriesTheIslandsComponentsAndPageWashesFromTheRender()
    {
        var handler = new CountingHandler(Rendered);
        var service = CreateService(handler);

        var result = await service.RenderAsync("<h/>", "<b/>", "<f/>", Home);

        _ = await Assert.That(result.Islands.Count).IsEqualTo(2);
        _ = await Assert.That(result.Islands[0].Name).IsEqualTo("AppHeader");
        _ = await Assert.That(result.Islands[0].Keys).IsEquivalentTo(["header", "header/nav"]);
        _ = await Assert.That(result.Islands[1].Name).IsEqualTo("RecipeSearchView");
        _ = await Assert.That(result.Islands[1].Props.GetProperty("query").GetString()).IsEqualTo("soup");
        _ = await Assert.That(result.Components).IsEquivalentTo(
            ["Features/Components/Header/AppHeader.Component.vue", "Features/Components/Footer/AppFooter.Component.vue"]);
        _ = await Assert.That(result.PageWashes).IsEqualTo(3);
    }

    [Test]
    public async Task RenderAsync_CacheHit_ReturnsTheCachedIslandsWithTheMarkup()
    {
        var handler = new CountingHandler(Rendered);
        var service = CreateService(handler);

        _ = await service.RenderAsync("<h/>", "<b/>", "<f/>", Home);
        var second = await service.RenderAsync("<h/>", "<b/>", "<f/>", Home);

        _ = await Assert.That(handler.Calls).IsEqualTo(1);
        _ = await Assert.That(second.Islands.Count).IsEqualTo(2);
        _ = await Assert.That(second.Components.Count).IsEqualTo(2);
        _ = await Assert.That(second.PageWashes).IsEqualTo(3);
    }

    [Test]
    public async Task RenderAsync_ARenderWithoutIslandFields_CarriesEmptyValues()
    {
        var handler = new CountingHandler();
        var service = CreateService(handler);

        var result = await service.RenderAsync("<h/>", "<b/>", "<f/>", Home);

        _ = await Assert.That(result.Html).IsEqualTo("<div></div>");
        _ = await Assert.That(result.Islands).IsEmpty();
        _ = await Assert.That(result.Components).IsEmpty();
        _ = await Assert.That(result.PageWashes).IsEqualTo(0);
    }

    [Test]
    public async Task RenderAsync_NoHtml_CarriesEmptyIslandValues()
    {
        var service = CreateService(new CountingHandler(), enabled: false);

        var result = await service.RenderAsync("<h/>", "<b/>", "<f/>", Home);

        _ = await Assert.That(result.Html).IsNull();
        _ = await Assert.That(result.Islands).IsEmpty();
        _ = await Assert.That(result.Components).IsEmpty();
        _ = await Assert.That(result.PageWashes).IsEqualTo(0);
    }

    private static readonly object Rendered = new
    {
        html = "<div></div>",
        schedule = "",
        renderTime = 1,
        islands = new object[]
        {
            new { name = "AppHeader", props = new { }, keys = new[] { "header", "header/nav" } },
            new { name = "RecipeSearchView", props = new { query = "soup" }, keys = Array.Empty<string>() },
        },
        components = new[] { "Features/Components/Header/AppHeader.Component.vue", "Features/Components/Footer/AppFooter.Component.vue" },
        pageWashes = 3,
    };

    private static VueSsrService CreateService(HttpMessageHandler handler, long cacheLimit = 1_000_000, bool enabled = true)
    {
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient("VueSsr")).Returns(() => new HttpClient(handler, disposeHandler: false) { BaseAddress = new Uri("http://ssr.test") });

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string> { ["VueSsr:Enabled"] = enabled ? "true" : "false" })
            .Build();

        var environment = new Mock<IHostEnvironment>();
        environment.SetupGet(e => e.EnvironmentName).Returns("Testing");

        return new VueSsrService(
            factory.Object,
            new SsrRenderCache(cacheLimit),
            configuration,
            environment.Object,
            NullLogger<VueSsrService>.Instance);
    }

    private sealed class CountingHandler(object body = null) : HttpMessageHandler
    {
        public const string Schedule = ".kcc-wash[data-wash=\"0a1b2c3d\"]{--wash-at:12.500s}";

        public int Calls { get; private set; }

        public string LastBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            LastBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            var response = JsonSerializer.Serialize(body ?? new { html = "<div></div>", schedule = Schedule, renderTime = 1 });
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(response, Encoding.UTF8, "application/json"),
            };
        }
    }
}
