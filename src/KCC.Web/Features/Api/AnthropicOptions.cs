namespace KCC.Web.Features.Api;

/// <summary>
/// Strongly-typed configuration for the Anthropic API integration.
/// Bind via <c>IConfiguration.GetSection("Anthropic")</c>.
/// </summary>
public class AnthropicOptions
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "Anthropic";

    /// <summary>Gets or sets the Anthropic API key. Set via user-secrets in dev, environment/config in prod.</summary>
    public string ApiKey { get; set; }

    /// <summary>Gets or sets the model id used to pick recipe icons.</summary>
    public string Model { get; set; } = "claude-haiku-4-5";
}
