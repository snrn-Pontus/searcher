using Microsoft.Extensions.Logging.Abstractions;
using Searcher.Models;
using Searcher.Options;
using Searcher.Providers;
using Searcher.Services;

namespace Searcher.Tests;

public sealed class ProviderConcurrencyLimiterTests
{
    [Fact]
    public async Task SearchAsync_limits_concurrent_provider_calls()
    {
        var inner = new TrackingProvider();
        var provider = new ProviderConcurrencyLimiter(
            inner,
            new ProviderConcurrencyGate(),
            Microsoft.Extensions.Options.Options.Create(
                new SearchEngineOptions { MaxConcurrentRequestsPerProvider = 2 }),
            NullLogger<ProviderConcurrencyLimiter>.Instance);

        var tasks = Enumerable.Range(0, 8)
            .Select(index => provider.SearchAsync($"term-{index}", CancellationToken.None));

        await Task.WhenAll(tasks);

        Assert.Equal(2, inner.MaxObservedConcurrency);
    }

    private sealed class TrackingProvider : ISearchProvider
    {
        private int _currentConcurrency;

        public string Name => "Tracked";
        public int MaxObservedConcurrency { get; private set; }

        public async Task<SearchTermResult> SearchAsync(string term, CancellationToken cancellationToken)
        {
            var current = Interlocked.Increment(ref _currentConcurrency);
            MaxObservedConcurrency = Math.Max(MaxObservedConcurrency, current);
            await Task.Delay(25, cancellationToken);
            Interlocked.Decrement(ref _currentConcurrency);
            return new SearchTermResult { Term = term, Hits = 1, Error = null };
        }
    }
}