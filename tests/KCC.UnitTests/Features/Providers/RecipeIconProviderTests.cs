using System.Net;
using System.Text;
using Anthropic;
using Anthropic.Core;
using KCC.Admin;
using KCC.Web.Features.Models.Options;
using KCC.Web.Features.Providers;
using Microsoft.Extensions.Logging.Abstractions;

namespace KCC.UnitTests.Features.Providers;

public class RecipeIconProviderTests
{
    private const string ModelIcon = "fa-duotone fa-egg-fried";

    [Test]
    public async Task WithoutAnApiKey_PicksTheFallbackIconWithoutCallingAnthropic()
    {
        var handler = new CountingHandler();
        var provider = new RecipeIconProvider(
            new AnthropicClient(new ClientOptions { ApiKey = string.Empty, HttpClient = new HttpClient(handler) }),
            new AnthropicOptions { ApiKey = string.Empty },
            NullLogger<RecipeIconProvider>.Instance);

        var icon = await provider.PickAsync("Shakshuka", "Eggs in tomato.", ["Eggs"], CancellationToken.None);

        _ = await Assert.That(icon).IsEqualTo(RecipeIcons.Fallback("Shakshuka"));
        _ = await Assert.That(handler.Calls).IsEqualTo(0);
    }

    [Test]
    public async Task ACancelledCaller_GetsItsCancellationInsteadOfTheFallback()
    {
        var handler = new SlowAnswerHandler(ModelIcon, TimeSpan.FromSeconds(30));
        var provider = new RecipeIconProvider(
            ClientOver(handler),
            new AnthropicOptions { ApiKey = "test-key" },
            NullLogger<RecipeIconProvider>.Instance,
            TimeSpan.FromMinutes(5));
        using var caller = new CancellationTokenSource();

        var pick = provider.PickAsync("Shakshuka", "Eggs in tomato.", ["Eggs"], caller.Token);
        await handler.Received.WaitAsync(TimeSpan.FromSeconds(10));
        await caller.CancelAsync();

        _ = await Assert.That(async () => await pick.WaitAsync(TimeSpan.FromSeconds(5))).Throws<OperationCanceledException>();
    }

    [Test]
    public async Task AnAnswerSlowerThanTheBudget_GivesTheFallback()
    {
        var handler = new SlowAnswerHandler(ModelIcon, TimeSpan.FromSeconds(5));
        var provider = new RecipeIconProvider(
            ClientOver(handler),
            new AnthropicOptions { ApiKey = "test-key" },
            NullLogger<RecipeIconProvider>.Instance,
            TimeSpan.FromMilliseconds(50));

        var icon = await provider.PickAsync("Shakshuka", "Eggs in tomato.", ["Eggs"], CancellationToken.None);

        _ = await Assert.That(icon).IsEqualTo(RecipeIcons.Fallback("Shakshuka"));
        _ = await Assert.That(handler.WasCancelled).IsTrue();
    }

    [Test]
    public async Task AnAnswerWhoseBodyStalls_GivesTheFallbackOnceTheBudgetPasses_AndIsDisposed()
    {
        var handler = new StallingBodyHandler();
        var provider = new RecipeIconProvider(
            ClientOver(handler),
            new AnthropicOptions { ApiKey = "test-key" },
            NullLogger<RecipeIconProvider>.Instance,
            TimeSpan.FromMilliseconds(50));

        var icon = await provider.PickAsync("Shakshuka", "Eggs in tomato.", ["Eggs"], CancellationToken.None)
            .WaitAsync(TimeSpan.FromSeconds(10));

        _ = await Assert.That(icon).IsEqualTo(RecipeIcons.Fallback("Shakshuka"));
        _ = await Assert.That(handler.BodyDisposed).IsTrue();
    }

    [Test]
    public async Task ClientOptions_CarryTheKeyWithANinetySecondTimeoutAndOneRetry()
    {
        var options = AnthropicClientOptionsFactory.Create(new AnthropicOptions { ApiKey = "test-key" });

        _ = await Assert.That(options.ApiKey).IsEqualTo("test-key");
        _ = await Assert.That(options.Timeout).IsEqualTo(TimeSpan.FromSeconds(90));
        _ = await Assert.That(options.MaxRetries).IsEqualTo(1);
    }

    private static AnthropicClient ClientOver(HttpMessageHandler handler) =>
        new(new ClientOptions { ApiKey = "test-key", HttpClient = new HttpClient(handler) });

    private sealed class CountingHandler : HttpMessageHandler
    {
        public int Calls { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromException<HttpResponseMessage>(new HttpRequestException("The recording handler refuses every request."));
        }
    }

    private sealed class SlowAnswerHandler(string icon, TimeSpan answerAfter) : HttpMessageHandler
    {
        private readonly TaskCompletionSource received = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task Received => received.Task;

        public bool WasCancelled { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            received.TrySetResult();
            try
            {
                await Task.Delay(answerAfter, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                WasCancelled = true;
                throw;
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    $$"""
                    {
                      "id": "msg_test",
                      "type": "message",
                      "role": "assistant",
                      "model": "claude-haiku-4-5",
                      "content": [
                        {
                          "type": "tool_use",
                          "id": "toolu_test",
                          "name": "select_icon",
                          "input": { "icon": "{{icon}}" },
                          "caller": { "type": "direct" }
                        }
                      ],
                      "stop_reason": "tool_use",
                      "stop_sequence": null,
                      "usage": { "input_tokens": 120, "output_tokens": 20 }
                    }
                    """,
                    Encoding.UTF8,
                    "application/json"),
            };
        }
    }
}
