using System.Diagnostics;
using Searcher.Models;
using Searcher.Providers;

namespace Searcher.Services;

public sealed class SearchService(
    IEnumerable<ISearchProvider> providers,
    ISearchQueryParser queryParser,
    ILogger<SearchService> logger) : ISearchService
{
    private readonly ISearchProvider[] _providers = providers.ToArray();

    public async Task<SearchResponse> SearchAsync(string query, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var terms = queryParser.Parse(query);

        if (_providers.Length == 0)
        {
            throw new InvalidOperationException("At least one search provider must be registered.");
        }

        var providerTasks = _providers.Select(provider => SearchProviderAsync(provider, terms, cancellationToken));
        var summaries = await Task.WhenAll(providerTasks);

        stopwatch.Stop();
        logger.LogInformation("Searched {TermCount} terms across {ProviderCount} providers in {ElapsedMilliseconds} ms.",
            terms.Count, _providers.Length, stopwatch.ElapsedMilliseconds);

        return new SearchResponse(query.Trim(), terms, summaries, stopwatch.ElapsedMilliseconds);
    }

    private static async Task<SearchProviderSummary> SearchProviderAsync(
        ISearchProvider provider,
        IReadOnlyList<string> terms,
        CancellationToken cancellationToken)
    {
        var termTasks = terms.Select(term => provider.SearchAsync(term, cancellationToken));
        var termResults = await Task.WhenAll(termTasks);
        var totalHits = termResults.Where(result => result.Hits.HasValue).Sum(result => result.Hits!.Value);
        var errors = termResults.Where(result => result.Error is not null).Select(result => result.Error).Distinct().ToArray();

        return new SearchProviderSummary(
            provider.Name,
            totalHits,
            errors.Length == 0,
            errors.Length == 0 ? null : string.Join(" ", errors),
            termResults);
    }
}
