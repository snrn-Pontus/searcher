using Microsoft.Extensions.Logging.Abstractions;
using Searcher.Models;
using Searcher.Options;
using Searcher.Providers;
using Searcher.Services;

namespace Searcher.Tests;

public sealed class SearchServiceTests
{
    [Fact]
    public async Task SearchAsync_sums_hits_per_provider()
    {
        var providers = new ISearchProvider[]
        {
            new StubProvider("One", term => new SearchTermResult(term, term.Length, null)),
            new StubProvider("Two", term => new SearchTermResult(term, 10, null))
        };
        var service = new SearchService(providers, new SearchQueryParser(), Microsoft.Extensions.Options.Options.Create(new ObservabilityOptions()), NullLogger<SearchService>.Instance);

        var response = await service.SearchAsync("hi world", CancellationToken.None);

        Assert.Equal(2, response.Providers.Count);
        Assert.Equal(7, response.Providers[0].TotalHits);
        Assert.Equal(20, response.Providers[1].TotalHits);
    }

    [Fact]
    public async Task SearchAsync_keeps_successful_results_when_a_provider_term_fails()
    {
        var provider = new StubProvider("Partial", term => term == "bad"
            ? new SearchTermResult(term, null, "Provider failed.")
            : new SearchTermResult(term, 4, null));
        var service = new SearchService([provider], new SearchQueryParser(), Microsoft.Extensions.Options.Options.Create(new ObservabilityOptions()), NullLogger<SearchService>.Instance);

        var response = await service.SearchAsync("good bad", CancellationToken.None);

        var summary = Assert.Single(response.Providers);
        Assert.False(summary.Succeeded);
        Assert.Equal(4, summary.TotalHits);
        Assert.Contains("Provider failed", summary.Error!);
    }

    private sealed class StubProvider(string name, Func<string, SearchTermResult> resultFactory) : ISearchProvider
    {
        public string Name { get; } = name;

        public Task<SearchTermResult> SearchAsync(string term, CancellationToken cancellationToken) =>
            Task.FromResult(resultFactory(term));
    }
}
