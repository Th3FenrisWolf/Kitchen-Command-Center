using System.Text.RegularExpressions;
using System.Web;

namespace KCC.Web.Features.Extensions;

public static class StringExtensions
{
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

    public static string StripTilde(this string input) => input?.TrimStart('~') ?? string.Empty;
}
