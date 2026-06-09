using System.Net.Http.Headers;
using System.Net.Http.Json;
using Searcher.Logging;
using Searcher.Models;
using Searcher.Options;

namespace Searcher.Providers;

public abstract class SearchProviderBase<TProviderResponse>(
    HttpClient httpClient,
    SearchProviderOptions options,
    ILogger logger) : ISearchProvider
    where TProviderResponse : class
{
    private const string ApiTokenHeader = "X-Api-Token";

    protected HttpClient HttpClient { get; } = httpClient;
    protected SearchProviderOptions Options { get; } = options;
    protected ILogger Logger { get; } = logger;
    protected virtual bool RequiresApiToken => true;

    public string Name => Options.DisplayName;

    public async Task<SearchTermResult> SearchAsync(string term, CancellationToken cancellationToken)
    {
        if (RequiresApiToken &&
            (string.IsNullOrWhiteSpace(Options.ApiToken) ||
             Options.ApiToken.Contains("replace", StringComparison.OrdinalIgnoreCase)))
        {
            AppLog.MissingApiToken(Logger, Name);
            return new SearchTermResult { Term = term, Hits = null, Error = $"{Name} is not configured correctly." };
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(Math.Clamp(Options.TimeoutSeconds, 1, 60)));

        try
        {
            using var request = CreateRequest(term);
            if (RequiresApiToken)
            {
                request.Headers.Add(ApiTokenHeader, Options.ApiToken);
            }
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using var response =
                await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
            if (!response.IsSuccessStatusCode)
            {
                AppLog.ProviderStatusCodeReturned(Logger, Name, (int)response.StatusCode, response.ReasonPhrase, term);
                return new SearchTermResult { Term = term, Hits = null, Error = $"{Name} could not complete the search." };
            }

            var providerResponse = await response.Content.ReadFromJsonAsync<TProviderResponse>(timeout.Token);
            if (providerResponse is null)
            {
                AppLog.EmptyProviderResponse(Logger, Name, term);
                return new SearchTermResult { Term = term, Hits = null, Error = $"{Name} returned an unreadable result." };
            }

            var hitCount = GetHitCount(providerResponse);
            if (hitCount.ProviderErrors.Count > 0)
            {
                AppLog.ProviderErrorsReturned(Logger, Name, hitCount.ProviderErrors.Count, term);
                return new SearchTermResult { Term = term, Hits = null, Error = $"{Name} could not complete the search." };
            }

            if (hitCount.TotalHits is null)
            {
                AppLog.UnsupportedProviderResponse(Logger, Name, term);
                return new SearchTermResult { Term = term, Hits = null, Error = $"{Name} returned an unreadable result." };
            }

            return new SearchTermResult { Term = term, Hits = hitCount.TotalHits.Value, Error = null };
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            AppLog.ProviderTimeout(Logger, Name, term);
            return new SearchTermResult { Term = term, Hits = null, Error = $"{Name} timed out while searching." };
        }
        catch (Exception ex)
        {
            AppLog.ProviderFailure(Logger, ex, Name, term);
            return new SearchTermResult { Term = term, Hits = null, Error = $"{Name} failed while searching." };
        }
    }

    protected abstract HttpRequestMessage CreateRequest(string term);
    protected abstract ProviderHitCountResult GetHitCount(TProviderResponse response);
}

public sealed record ProviderHitCountResult(long? TotalHits, IReadOnlyList<string> ProviderErrors);
