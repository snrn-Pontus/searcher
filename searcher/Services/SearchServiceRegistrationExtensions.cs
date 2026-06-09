using Searcher.Options;

namespace Searcher.Services;

public static class SearchServiceRegistrationExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddSearchOptions(IConfiguration configuration) =>
            services.AddOptions<SearchEngineOptions>()
                .Bind(configuration.GetSection(SearchEngineOptions.SectionName))
                .Validate(SearchEngineOptions.IsValid, "Search provider options are invalid.")
                .Services;

        public IServiceCollection AddSearchServices() =>
            services
                .AddSingleton<ISearchQueryParser, SearchQueryParser>()
                .AddSingleton<IProviderConcurrencyGate, ProviderConcurrencyGate>()
                .AddScoped<ISearchService, SearchService>();
    }
}
