using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Searcher.Options;
using Searcher.Providers;

namespace Searcher.Services;

public static class SearchProviderRegistrationExtensions
{
    public static IServiceCollection AddSearchProvider<TProvider>(this IServiceCollection services)
        where TProvider : class, ISearchProvider
    {
        services.AddHttpClient<TProvider>();
        services.AddScoped<ISearchProvider>(sp =>
            new CachedSearchProvider(
                new ProviderConcurrencyLimiter(
                    sp.GetRequiredService<TProvider>(),
                    sp.GetRequiredService<IProviderConcurrencyGate>(),
                    sp.GetRequiredService<IOptions<SearchEngineOptions>>(),
                    sp.GetRequiredService<ILogger<ProviderConcurrencyLimiter>>()),
                sp.GetRequiredService<IMemoryCache>(),
                sp.GetRequiredService<IOptions<SearchEngineOptions>>(),
                sp.GetRequiredService<ILogger<CachedSearchProvider>>()));

        return services;
    }
}
