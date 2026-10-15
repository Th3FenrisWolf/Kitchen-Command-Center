using Microsoft.Extensions.Configuration;

namespace KCC.ReaderEval;

public sealed class AnthropicKey
{
    public const string EnvironmentVariable = "ANTHROPIC_API_KEY";
    public const string SecretsKey = "Anthropic:ApiKey";

    private const string SecretsSource = "KCC.Web's user-secrets (Anthropic:ApiKey)";

    private AnthropicKey(string value, string source)
    {
        Value = value;
        Source = source;
    }

    public string Value { get; }

    public string Source { get; }

    public static AnthropicKey Find(IConfiguration userSecrets, Func<string, string> environment) =>
        userSecrets[SecretsKey] is { } secret && !string.IsNullOrWhiteSpace(secret) ? new(secret.Trim(), SecretsSource)
        : environment(EnvironmentVariable) is { } variable && !string.IsNullOrWhiteSpace(variable)
            ? new(variable.Trim(), EnvironmentVariable)
        : null;

    public override string ToString() => Source;
}
