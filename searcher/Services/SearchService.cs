using System.Diagnostics;
using Searcher.Logging;
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
        if (logger.IsEnabled(LogLevel.Debug))
        {
            AppLog.TermsSearched(logger, terms.Count, _providers.Length, stopwatch.ElapsedMilliseconds);
        }

        return new SearchResponse
        {
            Query = query.Trim(),
            Terms = terms,
            Providers = summaries,
            ElapsedMilliseconds = stopwatch.ElapsedMilliseconds
        };
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

        return new SearchProviderSummary
        {
            Provider = provider.Name,
            TotalHits = totalHits,
            Succeeded = errors.Length == 0,
            Error = errors.Length == 0 ? null : string.Join(" ", errors),
            Terms = termResults
        };
    }
}
