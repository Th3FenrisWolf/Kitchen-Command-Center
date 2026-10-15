using System.Globalization;
using System.Net;
using System.Text.Json;

namespace KCC.ReaderEval;

public sealed class ServiceErrorRecorder : DelegatingHandler
{
    public string LastError { get; private set; }

    public void Clear() => LastError = null;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            await response.Content.LoadIntoBufferAsync(cancellationToken);
            LastError = Described(response.StatusCode, await response.Content.ReadAsStringAsync(cancellationToken));
        }

        return response;
    }

    private static string Described(HttpStatusCode status, string body)
    {
        var described = string.Create(CultureInfo.InvariantCulture, $"HTTP {(int)status}");
        try
        {
            using var document = JsonDocument.Parse(body);
            var error = document.RootElement.GetProperty("error");
            return $"{described} {error.GetProperty("type").GetString()}: {error.GetProperty("message").GetString()}";
        }
        catch (Exception exception) when (exception is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            return described;
        }
    }
}
