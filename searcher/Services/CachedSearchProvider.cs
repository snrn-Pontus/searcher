using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Searcher.Models;
using Searcher.Options;
using Searcher.Providers;

namespace Searcher.Services;

public sealed class CachedSearchProvider(
    ISearchProvider innerProvider,
    IMemoryCache cache,
    IOptions<SearchEngineOptions> searchOptions,
    IOptions<ObservabilityOptions> observabilityOptions,
    ILogger<CachedSearchProvider> logger) : ISearchProvider
{
    public string Name => innerProvider.Name;

    public async Task<SearchTermResult> SearchAsync(string term, CancellationToken cancellationToken)
    {
        var cacheSeconds = searchOptions.Value.CacheDurationSeconds;
        var detailedLogging = observabilityOptions.Value.DetailedSearchLogging;
        if (cacheSeconds <= 0)
        {
            if (detailedLogging)
            {
                logger.LogDebug("Search cache disabled for provider {Provider}.", Name);
            }

            return await innerProvider.SearchAsync(term, cancellationToken);
        }

        var cacheKey = $"search:{Name}:{term}".ToLowerInvariant();
        if (cache.TryGetValue(cacheKey, out SearchTermResult? cachedResult) && cachedResult is not null)
        {
            if (detailedLogging)
            {
                logger.LogInformation("Search cache hit for provider {Provider} and term {Term}.", Name, term);
            }

            return cachedResult;
        }

        if (detailedLogging)
        {
            logger.LogInformation("Search cache miss for provider {Provider} and term {Term}.", Name, term);
        }

        var result = await innerProvider.SearchAsync(term, cancellationToken);
        if (result.Succeeded)
        {
            cache.Set(cacheKey, result, TimeSpan.FromSeconds(cacheSeconds));
            if (detailedLogging)
            {
                logger.LogDebug("Cached search result for provider {Provider} and term {Term} for {CacheSeconds} seconds.", Name, term, cacheSeconds);
            }
        }

        return result;
    }
}
