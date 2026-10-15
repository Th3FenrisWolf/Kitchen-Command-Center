using System.Net;

namespace KCC.UnitTests.Features.Providers;

internal sealed class StallingBodyHandler : HttpMessageHandler
{
    private readonly StallingBody body = new();

    public Task BodyStalled => body.Stalled;

    public bool BodyDisposed => body.Disposed;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
        Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(body) });

    private sealed class StallingBody() : MemoryStream("{\"id\":\"msg_test\",\"type\":\"message\","u8.ToArray())
    {
        private readonly TaskCompletionSource stalled = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task Stalled => stalled.Task;

        public bool Disposed { get; private set; }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (Position < Length)
            {
                return Read(buffer.Span);
            }

            stalled.TrySetResult();

            // Once the headers are in, the SDK reads the body on a token that nothing cancels.
            await Task.Delay(Timeout.InfiniteTimeSpan, CancellationToken.None);
            return 0;
        }

        protected override void Dispose(bool disposing)
        {
            Disposed = true;
            base.Dispose(disposing);
        }
    }
}
