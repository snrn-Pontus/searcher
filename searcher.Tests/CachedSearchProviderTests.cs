using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Searcher.Models;
using Searcher.Options;
using Searcher.Providers;
using Searcher.Services;

namespace Searcher.Tests;

public sealed class CachedSearchProviderTests
{
    [Fact]
    public async Task SearchAsync_caches_successful_provider_results()
    {
        var inner = new CountingProvider("Provider", new SearchTermResult("hello", 12, null));
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var provider = new CachedSearchProvider(
            inner,
            cache,
            Microsoft.Extensions.Options.Options.Create(new SearchEngineOptions { CacheDurationSeconds = 60 }),
            Microsoft.Extensions.Options.Options.Create(new ObservabilityOptions()),
            NullLogger<CachedSearchProvider>.Instance);

        var first = await provider.SearchAsync("hello", CancellationToken.None);
        var second = await provider.SearchAsync("hello", CancellationToken.None);

        Assert.Equal(12L, first.Hits.GetValueOrDefault());
        Assert.Equal(12L, second.Hits.GetValueOrDefault());
        Assert.False(first.FromCache);
        Assert.True(second.FromCache);
        Assert.Equal("hello", second.Term);
        Assert.Equal(1, inner.Calls);
    }

    [Fact]
    public async Task SearchAsync_does_not_cache_failed_provider_results()
    {
        var inner = new CountingProvider("Provider", new SearchTermResult("hello", null, "failed"));
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var provider = new CachedSearchProvider(
            inner,
            cache,
            Microsoft.Extensions.Options.Options.Create(new SearchEngineOptions { CacheDurationSeconds = 60 }),
            Microsoft.Extensions.Options.Options.Create(new ObservabilityOptions()),
            NullLogger<CachedSearchProvider>.Instance);

        await provider.SearchAsync("hello", CancellationToken.None);
        await provider.SearchAsync("hello", CancellationToken.None);

        Assert.Equal(2, inner.Calls);
    }

    private sealed class CountingProvider(string name, SearchTermResult result) : ISearchProvider
    {
        public int Calls { get; private set; }
        public string Name { get; } = name;

        public Task<SearchTermResult> SearchAsync(string term, CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(result with { Term = term });
        }
    }
}