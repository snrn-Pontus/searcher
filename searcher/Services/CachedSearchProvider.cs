using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Searcher.Logging;
using Searcher.Models;
using Searcher.Options;
using Searcher.Providers;

namespace Searcher.Services;

public sealed class CachedSearchProvider(
    ISearchProvider innerProvider,
    IMemoryCache cache,
    IOptions<SearchEngineOptions> searchOptions,
    ILogger<CachedSearchProvider> logger) : ISearchProvider
{
    public string Name => innerProvider.Name;

    public async Task<SearchTermResult> SearchAsync(string term, CancellationToken cancellationToken)
    {
        var cacheSeconds = searchOptions.Value.CacheDurationSeconds;
        if (cacheSeconds <= 0)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                AppLog.CacheDisabled(logger, Name);
            }

            return await innerProvider.SearchAsync(term, cancellationToken);
        }

        var cacheKey = $"search:{Name}:{term}".ToLowerInvariant();
        if (cache.TryGetValue(cacheKey, out SearchTermResult? cachedResult) && cachedResult is not null)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                AppLog.CacheHit(logger, Name, term);
            }

            return cachedResult with { Term = term, FromCache = true };
        }

        if (logger.IsEnabled(LogLevel.Debug))
        {
            AppLog.CacheMiss(logger, Name, term);
        }

        var result = await innerProvider.SearchAsync(term, cancellationToken);
        if (result.Succeeded)
        {
            cache.Set(cacheKey, result with { FromCache = false }, TimeSpan.FromSeconds(cacheSeconds));
            if (logger.IsEnabled(LogLevel.Debug))
            {
                AppLog.SearchResultCached(logger, Name, term, cacheSeconds);
            }
        }

        return result;
    }
}
