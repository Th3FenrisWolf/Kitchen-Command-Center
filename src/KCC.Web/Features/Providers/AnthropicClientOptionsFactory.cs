using Anthropic.Core;
using KCC.Web.Features.Models.Options;

namespace KCC.Web.Features.Providers;

public static class AnthropicClientOptionsFactory
{
    public static ClientOptions Create(AnthropicOptions anthropic) => new()
    {
        ApiKey = anthropic.ApiKey ?? string.Empty,
        Timeout = TimeSpan.FromSeconds(90),
        MaxRetries = 1,
    };
}
