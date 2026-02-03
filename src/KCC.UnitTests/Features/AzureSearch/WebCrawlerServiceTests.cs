using System.Net;
using CMS.Core;
using CMS.Websites;
using KCC.Web.Features.AzureSearch.Indexer;
using Moq;
using Moq.Protected;
using Xunit;

namespace KCC.UnitTests.Features.AzureSearch;

public class WebCrawlerServiceTests
{
    private readonly Mock<IEventLogService> mockLogService;
    private readonly Mock<IWebPageUrlRetriever> mockUrlRetriever;
    private readonly Mock<IAppSettingsService> mockAppSettingsService;
    private readonly WebCrawlerService crawlerService;

    public WebCrawlerServiceTests()
    {
        mockLogService = new Mock<IEventLogService>();
        mockUrlRetriever = new Mock<IWebPageUrlRetriever>();
        mockAppSettingsService = new Mock<IAppSettingsService>();

        mockAppSettingsService
            .Setup(x => x["WebCrawlerBaseUrl"])
            .Returns("https://example.com");

        // Create a mocked HttpMessageHandler to simulate HTTP responses.
        var httpMessageHandler = new Mock<HttpMessageHandler>();
        httpMessageHandler
            .Protected() // Access the protected method SendAsync.
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("Mocked web page content")
            });

        var httpClient = new HttpClient(httpMessageHandler.Object);
        crawlerService = new WebCrawlerService(
            httpClient,
            mockLogService.Object,
            mockUrlRetriever.Object,
            mockAppSettingsService.Object);
    }

    [Fact]
    public async Task CrawlWebPage_ReturnsContent_WhenUrlIsValid()
    {
        // Arrange
        var systemFields = new WebPageFields { WebPageItemTreePath = "/some/path" };
        var mockPage = new PageBuilderPage() { SystemFields = systemFields };
        var mockUrl = new WebPageUrl("/some/path", "https://example.com");
        mockUrlRetriever
            .Setup(retriever => retriever.Retrieve(It.IsAny<IWebPageFieldsSource>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUrl);

        // Act
        var result = await crawlerService.CrawlPage(mockPage);

        // Assert
        Assert.Equal("Mocked web page content", result);
    }

    [Fact]
    public async Task CrawlWebPage_LogsException_WhenExceptionOccurs()
    {

        // Arrange
        var systemFields = new WebPageFields { WebPageItemTreePath = "/some/path" };
        var mockPage = new PageBuilderPage() { SystemFields = systemFields };
        mockUrlRetriever
            .Setup(retriever => retriever.Retrieve(It.IsAny<IWebPageFieldsSource>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Mocked exception"));

        // Act
        var result = await crawlerService.CrawlPage(mockPage);

        // Assert
        Assert.Equal(string.Empty, result);
        mockLogService.Verify(log => log.LogEvent(It.IsAny<EventLogData>()), Times.Once);
    }

    [Fact]
    public async Task CrawlPage_ReturnsContent_WhenUrlIsValid()
    {
        // Arrange
        var url = "page";

        // Act
        var result = await crawlerService.CrawlPage(url);

        // Assert
        Assert.Equal("Mocked web page content", result);
    }

    [Fact]
    public async Task CrawlPage_LogsException_WhenExceptionOccurs()
    {
        // Arrange
        var url = "invalid-page";
        var httpMessageHandler = new Mock<HttpMessageHandler>();
        httpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ThrowsAsync(new HttpRequestException("Mocked HTTP request exception"));

        var failingHttpClient = new HttpClient(httpMessageHandler.Object);
        var failingCrawlerService = new WebCrawlerService(
            failingHttpClient,
            mockLogService.Object,
            mockUrlRetriever.Object,
            mockAppSettingsService.Object);

        // Act
        var result = await failingCrawlerService.CrawlPage(url);

        // Assert
        Assert.Equal(string.Empty, result);
        mockLogService.Verify(log => log.LogEvent(It.IsAny<EventLogData>()), Times.Once);
    }
}