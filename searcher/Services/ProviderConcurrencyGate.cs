using System.Collections.Concurrent;

namespace Searcher.Services;

public sealed class ProviderConcurrencyGate : IProviderConcurrencyGate
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _semaphores = new(StringComparer.OrdinalIgnoreCase);

    public async Task<IDisposable> EnterAsync(string providerName, int maxConcurrency,
        CancellationToken cancellationToken)
    {
        var semaphore = _semaphores.GetOrAdd(providerName, _ => new SemaphoreSlim(maxConcurrency));
        await semaphore.WaitAsync(cancellationToken);
        return new Releaser(semaphore);
    }

    private sealed class Releaser(SemaphoreSlim semaphore) : IDisposable
    {
        public void Dispose() => semaphore.Release();
    }
}