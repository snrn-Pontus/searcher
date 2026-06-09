using System.Diagnostics;
using Microsoft.Extensions.Options;
using Searcher.Logging;
using Searcher.Models;
using Searcher.Options;
using Searcher.Providers;

namespace Searcher.Services;

public sealed class ProviderConcurrencyLimiter(
    ISearchProvider innerProvider,
    IProviderConcurrencyGate concurrencyGate,
    IOptions<SearchEngineOptions> searchOptions,
    ILogger<ProviderConcurrencyLimiter> logger) : ISearchProvider
{
    public string Name => innerProvider.Name;

    public async Task<SearchTermResult> SearchAsync(string term, CancellationToken cancellationToken)
    {
        var shouldMeasure = logger.IsEnabled(LogLevel.Debug) || logger.IsEnabled(LogLevel.Warning);
        var stopwatch = shouldMeasure ? Stopwatch.StartNew() : null;

        using var lease = await concurrencyGate.EnterAsync(
            Name,
            searchOptions.Value.MaxConcurrentRequestsPerProvider,
            cancellationToken);

        if (logger.IsEnabled(LogLevel.Debug) && stopwatch is not null)
        {
            var waitMilliseconds = stopwatch.ElapsedMilliseconds;
            if (waitMilliseconds > 0)
            {
                AppLog.ProviderSlotWaited(logger, waitMilliseconds, Name);
            }
        }

        var result = await innerProvider.SearchAsync(term, cancellationToken);

        if (stopwatch is null)
        {
            return result;
        }

        stopwatch.Stop();
        if (result.Succeeded)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                AppLog.ProviderTermCompleted(logger, Name, term, result.Hits.GetValueOrDefault(), stopwatch.ElapsedMilliseconds);
            }
        }
        else if (logger.IsEnabled(LogLevel.Warning))
        {
            AppLog.ProviderTermFailed(logger, Name, term, stopwatch.ElapsedMilliseconds, result.Error);
        }

        return result;
    }
}
