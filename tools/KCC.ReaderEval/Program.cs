using Anthropic;
using KCC.ReaderEval;
using KCC.Web.Features.Models.Options;
using KCC.Web.Features.Providers;
using KCC.Web.Features.RecipeImport;
using Microsoft.Extensions.Configuration;

if (!EvalOptions.TryParse(args, out var options, out var error))
{
    Console.Error.WriteLine(error);
    Console.Error.WriteLine(EvalOptions.Usage);
    return 2;
}

IReadOnlyList<EvalCase> cases = [];
RecipeCatalog catalog = null;
if (options.Mode != EvalMode.Check)
{
    cases = EvalSet.Load(EvalSet.DefaultDirectory(), out var problem);
    catalog = problem is null ? EvalSet.LoadCatalog(EvalSet.DefaultDirectory(), out problem) : null;
    if (problem is not null)
    {
        Console.Error.WriteLine(problem);
        return 2;
    }
}

var userSecrets = new ConfigurationBuilder()
    .AddUserSecrets(typeof(AnthropicRecipePhotoReader).Assembly, optional: true)
    .Build();
if (AnthropicKey.Find(userSecrets, Environment.GetEnvironmentVariable) is not { } key)
{
    Console.Error.WriteLine(
        $"No Anthropic key: set {AnthropicKey.SecretsKey} in KCC.Web's user-secrets, "
        + $"or {AnthropicKey.EnvironmentVariable}.");
    return 2;
}

Console.WriteLine($"Using the Anthropic key from {key.Source}.");
using var stop = new CancellationTokenSource();
Console.CancelKeyPress += (_, keyPress) =>
{
    keyPress.Cancel = true;
    stop.Cancel();
};

var anthropic = new AnthropicOptions { ApiKey = key.Value };
var recorder = new ServiceErrorRecorder { InnerHandler = new SocketsHttpHandler() };
using var client = new AnthropicClient(
    AnthropicClientOptionsFactory.Create(anthropic) with { HttpClient = new HttpClient(recorder) });
var runner = new EvalRunner(
    new AnthropicEvalService(client, anthropic, recorder, TimeProvider.System),
    Console.Out,
    question => Confirmation.Ask(Console.In, Console.Out, question),
    catalog);

try
{
    return options.Mode switch
    {
        EvalMode.Check => await runner.CheckAsync(options.Models, stop.Token),
        EvalMode.Draft => await runner.DraftAsync(cases, options.Models[0], options.Edge, stop.Token),
        _ => await runner.CompareAsync(cases, options.Models, options.Edge, stop.Token) is null ? 1 : 0,
    };
}
catch (OperationCanceledException) when (stop.IsCancellationRequested)
{
    Console.Error.WriteLine("Stopped. A read that was running is still billed.");
    return 130;
}
