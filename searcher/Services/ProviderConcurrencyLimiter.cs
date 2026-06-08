using System.Diagnostics;
using Microsoft.Extensions.Options;
using Searcher.Models;
using Searcher.Options;
using Searcher.Providers;

namespace Searcher.Services;

public sealed class ProviderConcurrencyLimiter(
    ISearchProvider innerProvider,
    IProviderConcurrencyGate concurrencyGate,
    IOptions<SearchEngineOptions> options,
    ILogger<ProviderConcurrencyLimiter> logger) : ISearchProvider
{
    public string Name => innerProvider.Name;

    public async Task<SearchTermResult> SearchAsync(string term, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        using var lease = await concurrencyGate.EnterAsync(
            Name,
            options.Value.MaxConcurrentRequestsPerProvider,
            cancellationToken);

        var waitMilliseconds = stopwatch.ElapsedMilliseconds;
        if (waitMilliseconds > 0)
        {
            logger.LogDebug("Waited {WaitMilliseconds} ms for provider {Provider} concurrency slot.", waitMilliseconds, Name);
        }

        var result = await innerProvider.SearchAsync(term, cancellationToken);
        stopwatch.Stop();

        if (result.Succeeded)
        {
            logger.LogInformation(
                "Provider {Provider} completed term {Term} with {Hits} hits in {ElapsedMilliseconds} ms.",
                Name,
                term,
                result.Hits,
                stopwatch.ElapsedMilliseconds);
        }
        else
        {
            logger.LogWarning(
                "Provider {Provider} failed term {Term} in {ElapsedMilliseconds} ms: {Error}",
                Name,
                term,
                stopwatch.ElapsedMilliseconds,
                result.Error);
        }

        return result;
    }
}
