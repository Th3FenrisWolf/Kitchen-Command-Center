using System;
using System.Text.RegularExpressions;
using System.Web;

namespace KitchenCommandCenter.Web.Extensions;

public static class StringExtensions
{
    public static string StripHtmlTags(this string input) => Regex.Replace(input, "\\<[^\\>]*\\>", string.Empty);

    public static string StripParagraphTag(this string input) =>
        string.IsNullOrWhiteSpace(input)
            ? string.Empty
            : Regex.Match(input, @"<p\b[^>]*>(.*?)<\/p>", RegexOptions.IgnoreCase | RegexOptions.Singleline).Groups[1].Value;

    public static string ToKebabCase(this string str) =>
        // Return empty string if it's null or empty
        string.IsNullOrWhiteSpace(str) ? string.Empty :
            // Convert PascalCase to kebab-case
            Regex.Replace(str, "(?<!^)([A-Z])", "-$1").ToLower();

    public static string RemoveQueryStringParameter(this string url, string parameterToRemove)
    {
        // Ensure the URL is absolute by providing a dummy base if necessary
        var uriBuilder = new UriBuilder(new Uri(new Uri("http://example.com"), url));

        var queryString = HttpUtility.ParseQueryString(uriBuilder.Query);

        // Remove the desired query string parameter
        queryString.Remove(parameterToRemove);

        // Reconstruct the URL without the removed parameter
        uriBuilder.Query = queryString.ToString() ?? string.Empty;

        // If the original URL was absolute, return the absolute URL; otherwise, return the relative URL
        return url.StartsWith("http", StringComparison.OrdinalIgnoreCase)
            ? uriBuilder.Uri.ToString()
            : uriBuilder.Uri.PathAndQuery;
    }

    // static method to add a query string parameter to a URL. if the parameter already exists, it is replaced with the new value
    public static string AddQueryStringParameter(this string url, string parameterName, string parameterValue, string hash = null)
    {
        // Ensure the URL is absolute by providing a dummy base if necessary
        var uriBuilder = new UriBuilder(new Uri(new Uri("http://example.com"), url));
        var queryString = HttpUtility.ParseQueryString(uriBuilder.Query);

        // Remove the desired query string parameter
        queryString.Remove(parameterName);

        // Add the desired query string parameter
        queryString.Add(parameterName, parameterValue);

        // Reconstruct the URL with the new parameter
        uriBuilder.Query = queryString.ToString() ?? string.Empty;

        if (!string.IsNullOrEmpty(hash))
        {
            uriBuilder.Fragment = hash;
        }

        // Remove the leading "?" if there is no query string
        if (string.IsNullOrEmpty(uriBuilder.Query))
        {
            return string.IsNullOrWhiteSpace(uriBuilder.Fragment) ? uriBuilder.Path : $"{uriBuilder.Path}{uriBuilder.Fragment}";
        }

        // Remove the leading "#" if there is no fragment
        if (string.IsNullOrEmpty(uriBuilder.Fragment))
        {
            return string.IsNullOrWhiteSpace(uriBuilder.Query) ? uriBuilder.Path : $"{uriBuilder.Path}{uriBuilder.Query}";
        }

        return $"{uriBuilder.Path}{uriBuilder.Query}{uriBuilder.Fragment}";
    }

    public static string TruncateAndAddEllipsis(this string input, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        if (input.Length <= maxLength)
        {
            return input;
        }

        return input.Substring(0, maxLength) + "...";
    }

    public static string StripTilde(this string input) => input?.TrimStart('~') ?? string.Empty;
}
