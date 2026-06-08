namespace Searcher.Services;

public interface IProviderConcurrencyGate
{
    Task<IDisposable> EnterAsync(string providerName, int maxConcurrency, CancellationToken cancellationToken);
}
