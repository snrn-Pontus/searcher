namespace Searcher.Logging;

public static class AppLog
{
    private static readonly Action<ILogger, Exception?> RejectedEmptySearchRequest =
        LoggerMessage.Define(LogLevel.Warning, new EventId(1000, nameof(RejectedEmptySearchRequest)),
            "Rejected empty search request.");

    private static readonly Action<ILogger, int, Exception?> ReceivedSearchRequest =
        LoggerMessage.Define<int>(LogLevel.Debug, new EventId(1001, nameof(ReceivedSearchRequest)),
            "Received search request with {QueryLength} characters.");

    private static readonly Action<ILogger, int, int, long, Exception?> CompletedSearchRequest =
        LoggerMessage.Define<int, int, long>(LogLevel.Information, new EventId(1002, nameof(CompletedSearchRequest)),
            "Completed search request for {TermCount} terms across {ProviderCount} providers in {ElapsedMilliseconds} ms.");

    private static readonly Action<ILogger, Exception?> RejectedInvalidSearchRequest =
        LoggerMessage.Define(LogLevel.Warning, new EventId(1003, nameof(RejectedInvalidSearchRequest)),
            "Rejected invalid search request.");

    private static readonly Action<ILogger, int, int, long, Exception?> SearchedTerms =
        LoggerMessage.Define<int, int, long>(LogLevel.Debug, new EventId(2000, nameof(SearchedTerms)),
            "Searched {TermCount} terms across {ProviderCount} providers in {ElapsedMilliseconds} ms.");

    private static readonly Action<ILogger, long, string, Exception?> WaitedForProviderSlot =
        LoggerMessage.Define<long, string>(LogLevel.Debug, new EventId(3000, nameof(WaitedForProviderSlot)),
            "Waited {WaitMilliseconds} ms for provider {Provider} concurrency slot.");

    private static readonly Action<ILogger, string, string, long, long, Exception?> ProviderCompletedTerm =
        LoggerMessage.Define<string, string, long, long>(LogLevel.Debug, new EventId(3001, nameof(ProviderCompletedTerm)),
            "Provider {Provider} completed term {Term} with {Hits} hits in {ElapsedMilliseconds} ms.");

    private static readonly Action<ILogger, string, string, long, string?, Exception?> ProviderFailedTerm =
        LoggerMessage.Define<string, string, long, string?>(LogLevel.Warning, new EventId(3002, nameof(ProviderFailedTerm)),
            "Provider {Provider} failed term {Term} in {ElapsedMilliseconds} ms: {Error}");

    private static readonly Action<ILogger, string, Exception?> SearchCacheDisabled =
        LoggerMessage.Define<string>(LogLevel.Debug, new EventId(4000, nameof(SearchCacheDisabled)),
            "Search cache disabled for provider {Provider}.");

    private static readonly Action<ILogger, string, string, Exception?> SearchCacheHit =
        LoggerMessage.Define<string, string>(LogLevel.Debug, new EventId(4001, nameof(SearchCacheHit)),
            "Search cache hit for provider {Provider} and term {Term}.");

    private static readonly Action<ILogger, string, string, Exception?> SearchCacheMiss =
        LoggerMessage.Define<string, string>(LogLevel.Debug, new EventId(4002, nameof(SearchCacheMiss)),
            "Search cache miss for provider {Provider} and term {Term}.");

    private static readonly Action<ILogger, string, string, int, Exception?> CachedSearchResult =
        LoggerMessage.Define<string, string, int>(LogLevel.Debug, new EventId(4003, nameof(CachedSearchResult)),
            "Cached search result for provider {Provider} and term {Term} for {CacheSeconds} seconds.");

    private static readonly Action<ILogger, string, Exception?> ProviderMissingApiToken =
        LoggerMessage.Define<string>(LogLevel.Warning, new EventId(5000, nameof(ProviderMissingApiToken)),
            "Provider {Provider} is missing an API token.");

    private static readonly Action<ILogger, string, int, string?, string, Exception?> ProviderReturnedStatusCode =
        LoggerMessage.Define<string, int, string?, string>(LogLevel.Warning, new EventId(5001, nameof(ProviderReturnedStatusCode)),
            "Provider {Provider} returned {StatusCode} {ReasonPhrase} for term {Term}.");

    private static readonly Action<ILogger, string, string, Exception?> ProviderReturnedEmptyResponse =
        LoggerMessage.Define<string, string>(LogLevel.Warning, new EventId(5002, nameof(ProviderReturnedEmptyResponse)),
            "Provider {Provider} returned an empty response for term {Term}.");

    private static readonly Action<ILogger, string, int, string, Exception?> ProviderReturnedErrors =
        LoggerMessage.Define<string, int, string>(LogLevel.Warning, new EventId(5003, nameof(ProviderReturnedErrors)),
            "Provider {Provider} returned {ErrorCount} provider errors for term {Term}.");

    private static readonly Action<ILogger, string, string, Exception?> ProviderReturnedUnsupportedResponse =
        LoggerMessage.Define<string, string>(LogLevel.Warning, new EventId(5004, nameof(ProviderReturnedUnsupportedResponse)),
            "Provider {Provider} returned an unsupported response format for term {Term}.");

    private static readonly Action<ILogger, string, string, Exception?> ProviderTimedOut =
        LoggerMessage.Define<string, string>(LogLevel.Warning, new EventId(5005, nameof(ProviderTimedOut)),
            "Provider {Provider} timed out for term {Term}.");

    private static readonly Action<ILogger, string, string, Exception?> ProviderFailed =
        LoggerMessage.Define<string, string>(LogLevel.Warning, new EventId(5006, nameof(ProviderFailed)),
            "Provider {Provider} failed for term {Term}.");

    public static void RejectedEmptySearch(ILogger logger) => RejectedEmptySearchRequest(logger, null);
    public static void SearchRequestReceived(ILogger logger, int queryLength) => ReceivedSearchRequest(logger, queryLength, null);
    public static void SearchRequestCompleted(ILogger logger, int termCount, int providerCount, long elapsedMilliseconds) => CompletedSearchRequest(logger, termCount, providerCount, elapsedMilliseconds, null);
    public static void InvalidSearchRejected(ILogger logger, Exception exception) => RejectedInvalidSearchRequest(logger, exception);
    public static void TermsSearched(ILogger logger, int termCount, int providerCount, long elapsedMilliseconds) => SearchedTerms(logger, termCount, providerCount, elapsedMilliseconds, null);
    public static void ProviderSlotWaited(ILogger logger, long waitMilliseconds, string provider) => WaitedForProviderSlot(logger, waitMilliseconds, provider, null);
    public static void ProviderTermCompleted(ILogger logger, string provider, string term, long hits, long elapsedMilliseconds) => ProviderCompletedTerm(logger, provider, term, hits, elapsedMilliseconds, null);
    public static void ProviderTermFailed(ILogger logger, string provider, string term, long elapsedMilliseconds, string? error) => ProviderFailedTerm(logger, provider, term, elapsedMilliseconds, error, null);
    public static void CacheDisabled(ILogger logger, string provider) => SearchCacheDisabled(logger, provider, null);
    public static void CacheHit(ILogger logger, string provider, string term) => SearchCacheHit(logger, provider, term, null);
    public static void CacheMiss(ILogger logger, string provider, string term) => SearchCacheMiss(logger, provider, term, null);
    public static void SearchResultCached(ILogger logger, string provider, string term, int cacheSeconds) => CachedSearchResult(logger, provider, term, cacheSeconds, null);
    public static void MissingApiToken(ILogger logger, string provider) => ProviderMissingApiToken(logger, provider, null);
    public static void ProviderStatusCodeReturned(ILogger logger, string provider, int statusCode, string? reasonPhrase, string term) => ProviderReturnedStatusCode(logger, provider, statusCode, reasonPhrase, term, null);
    public static void EmptyProviderResponse(ILogger logger, string provider, string term) => ProviderReturnedEmptyResponse(logger, provider, term, null);
    public static void ProviderErrorsReturned(ILogger logger, string provider, int errorCount, string term) => ProviderReturnedErrors(logger, provider, errorCount, term, null);
    public static void UnsupportedProviderResponse(ILogger logger, string provider, string term) => ProviderReturnedUnsupportedResponse(logger, provider, term, null);
    public static void ProviderTimeout(ILogger logger, string provider, string term) => ProviderTimedOut(logger, provider, term, null);
    public static void ProviderFailure(ILogger logger, Exception exception, string provider, string term) => ProviderFailed(logger, provider, term, exception);
}
