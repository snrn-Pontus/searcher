using System.Net.Http.Headers;
using Searcher.Models;
using Searcher.Options;

namespace Searcher.Providers;

public abstract class SearchProviderBase(HttpClient httpClient, SearchProviderOptions options, ILogger logger)
    : ISearchProvider
{
    private const string ApiTokenHeader = "X-Api-Token";

    protected HttpClient HttpClient { get; } = httpClient;
    protected SearchProviderOptions Options { get; } = options;
    protected ILogger Logger { get; } = logger;

    public string Name => Options.DisplayName;

    public async Task<SearchTermResult> SearchAsync(string term, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(Options.ApiToken) ||
            Options.ApiToken.Contains("replace", StringComparison.OrdinalIgnoreCase))
        {
            Logger.LogWarning("Provider {Provider} is missing an API token.", Name);
            return new SearchTermResult(term, null, $"{Name} is not configured correctly.");
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(Math.Clamp(Options.TimeoutSeconds, 1, 60)));

        try
        {
            using var request = CreateRequest(term);
            request.Headers.Add(ApiTokenHeader, Options.ApiToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using var response =
                await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
            if (!response.IsSuccessStatusCode)
            {
                Logger.LogWarning("Provider {Provider} returned {StatusCode} {ReasonPhrase} for term {Term}.",
                    Name,
                    (int)response.StatusCode,
                    response.ReasonPhrase,
                    term);
                return new SearchTermResult(term, null, $"{Name} could not complete the search.");
            }

            var hitCount = await ReadHitCountAsync(response.Content, timeout.Token);
            if (hitCount.ProviderErrors.Count > 0)
            {
                Logger.LogWarning("Provider {Provider} returned {ErrorCount} provider errors for term {Term}.",
                    Name,
                    hitCount.ProviderErrors.Count,
                    term);
                return new SearchTermResult(term, null, $"{Name} could not complete the search.");
            }

            if (hitCount.TotalHits is null)
            {
                Logger.LogWarning("Provider {Provider} returned an unsupported response format for term {Term}.", Name,
                    term);
                return new SearchTermResult(term, null, $"{Name} returned an unreadable result.");
            }

            return new SearchTermResult(term, hitCount.TotalHits.Value, null);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            Logger.LogWarning("Provider {Provider} timed out for term {Term}.", Name, term);
            return new SearchTermResult(term, null, $"{Name} timed out while searching.");
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Provider {Provider} failed for term {Term}.", Name, term);
            return new SearchTermResult(term, null, $"{Name} failed while searching.");
        }
    }

    protected abstract HttpRequestMessage CreateRequest(string term);

    protected abstract Task<ProviderHitCountResult> ReadHitCountAsync(HttpContent content,
        CancellationToken cancellationToken);
}

public sealed record ProviderHitCountResult(long? TotalHits, IReadOnlyList<string> ProviderErrors)
{
    public static ProviderHitCountResult Empty { get; } = new(null, []);
}