using KCC.Web.Features.RecipeImport;

namespace KCC.IntegrationTests.Config;

// Delegating, rather than staying off, keeps every test that scripts nothing on the composer's own reader.
public sealed class FakeRecipePhotoReader(IRecipePhotoReader? composerReader) : IRecipePhotoReader
{
    private readonly Lock gate = new();
    private readonly List<ReadRequest> calls = [];
    private Func<CancellationToken, Task<ReadAttempt>>? scriptedRead;
    private HeldRead? heldRead;

    public bool IsConfigured
    {
        get
        {
            lock (gate)
            {
                return scriptedRead is not null || composerReader?.IsConfigured == true;
            }
        }
    }

    public IReadOnlyList<ReadRequest> Calls
    {
        get
        {
            lock (gate)
            {
                return [.. calls];
            }
        }
    }

    public void SwitchOn() => Script(_ => throw new InvalidOperationException("The fake reader is on, but the test scripted no read."));

    public void Answer(ReadAttempt attempt) => Script(_ => Task.FromResult(attempt));

    public HeldRead HoldOpen()
    {
        var held = new HeldRead();
        Script(held.WaitAsync, held);
        return held;
    }

    public void Reset()
    {
        lock (gate)
        {
            heldRead?.Release(ReadAttempt.NoAnswer());
            heldRead = null;
            scriptedRead = null;
            calls.Clear();
        }
    }

    public Task<ReadAttempt> ReadAsync(ReadRequest request, CancellationToken cancellationToken)
    {
        Func<CancellationToken, Task<ReadAttempt>>? read;
        lock (gate)
        {
            calls.Add(request);
            read = scriptedRead;
        }

        return read is not null ? read(cancellationToken)
            : composerReader is not null ? composerReader.ReadAsync(request, cancellationToken)
            : throw new InvalidOperationException("No reader is registered for the fake reader to hand the call to.");
    }

    private void Script(Func<CancellationToken, Task<ReadAttempt>> read, HeldRead? held = null)
    {
        lock (gate)
        {
            heldRead?.Release(ReadAttempt.NoAnswer());
            heldRead = held;
            scriptedRead = read;
        }
    }
}

public sealed class HeldRead
{
    private readonly TaskCompletionSource started = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource<ReadAttempt> attempt = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task Started => started.Task;

    public void Release(ReadAttempt answer) => attempt.TrySetResult(answer);

    internal Task<ReadAttempt> WaitAsync(CancellationToken cancellationToken)
    {
        started.TrySetResult();
        return attempt.Task.WaitAsync(cancellationToken);
    }
}
