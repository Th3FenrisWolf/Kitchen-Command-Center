using KCC.Web.Features.AzureSearch.Indexer;
using Xunit;

namespace KCC.UnitTests.Features.AzureSearch;

public class HtmlSanitizerTests
{
    [Fact]
    public void SanitizeHtmlDocument_ReturnsEmptyString_WhenHtmlContentIsNullOrWhitespace()
    {
        // Arrange
        var sanitizer = new HtmlSanitizer();

        // Act
        var result1 = sanitizer.SanitizeHtmlDocument(null);
        var result2 = sanitizer.SanitizeHtmlDocument("");
        var result3 = sanitizer.SanitizeHtmlDocument("   ");

        // Assert
        Assert.Equal(string.Empty, result1);
        Assert.Equal(string.Empty, result2);
        Assert.Equal(string.Empty, result3);
    }

    [Fact]
    public void SanitizeHtmlDocument_RemovesScriptsAndStyles()
    {
        // Arrange
        var sanitizer = new HtmlSanitizer();
        var htmlContent = @"
            <html>
                <head>
                    <title>Test Title</title>
                    <meta name='description' content='Test Description'>
                </head>
                <body>
                    <script>alert('test');</script>
                    <style>body { background-color: red; }</style>
                    <p>Hello World</p>
                </body>
            </html>";

        // Act
        var result = sanitizer.SanitizeHtmlDocument(htmlContent);

        // Assert
        Assert.DoesNotContain("alert('test')", result);
        Assert.DoesNotContain("background-color", result);
        Assert.Contains("Test Title", result);
        Assert.Contains("Test Description", result);
        Assert.Contains("Hello World", result);
    }

    [Fact]
    public void SanitizeHtmlDocument_RemovesExcludedElements()
    {
        // Arrange
        var sanitizer = new HtmlSanitizer();
        var htmlContent = @"
            <html>
                <body>
                    <p data-ktc-search-exclude='true'>This should be removed</p>
                    <p>This should stay</p>
                </body>
            </html>";

        // Act
        var result = sanitizer.SanitizeHtmlDocument(htmlContent);

        // Assert
        Assert.DoesNotContain("This should be removed", result);
        Assert.Contains("This should stay", result);
    }

    [Fact]
    public void SanitizeHtmlDocument_RemovesHeaderAndFooter()
    {
        // Arrange
        var sanitizer = new HtmlSanitizer();
        var htmlContent = @"
            <html>
                <body>
                    <header>This is a header</header>
                    <p>Main content</p>
                    <footer>This is a footer</footer>
                </body>
            </html>";

        // Act
        var result = sanitizer.SanitizeHtmlDocument(htmlContent);

        // Assert
        Assert.DoesNotContain("This is a header", result);
        Assert.DoesNotContain("This is a footer", result);
        Assert.Contains("Main content", result);
    }

    [Fact]
    public void SanitizeHtmlDocument_NormalizesWhitespace()
    {
        // Arrange
        var sanitizer = new HtmlSanitizer();
        var htmlContent = "<html><body><p>   Text   with   irregular   spacing.   </p></body></html>";

        // Act
        var result = sanitizer.SanitizeHtmlDocument(htmlContent);

        // Assert
        Assert.Equal("Text with irregular spacing.", result);
    }

    [Fact]
    public void SanitizeHtmlDocument_ExtractsTitleAndDescription()
    {
        // Arrange
        var sanitizer = new HtmlSanitizer();
        var htmlContent = @"
            <html>
                <head>
                    <title>Sample Title</title>
                    <meta name='description' content='Sample Description'>
                </head>
                <body>
                    <p>Content goes here.</p>
                </body>
            </html>";

        // Act
        var result = sanitizer.SanitizeHtmlDocument(htmlContent);

        // Assert
        Assert.Contains("Sample Title", result);
        Assert.Contains("Sample Description", result);
        Assert.Contains("Content goes here.", result);
    }
}
