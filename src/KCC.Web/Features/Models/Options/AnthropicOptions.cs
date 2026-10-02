namespace KCC.Web.Features.Models.Options;

public class AnthropicOptions
{
    public const string SectionName = "Anthropic";

    public string ApiKey { get; set; }

    public string Model { get; set; } = "claude-haiku-4-5";
}
