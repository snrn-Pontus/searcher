using Microsoft.Extensions.Options;
using Searcher.Options;

namespace Searcher.Providers;

public sealed class AltavistaSearchProvider(
    HttpClient httpClient,
    IOptions<SearchEngineOptions> options,
    ILogger<AltavistaSearchProvider> logger)
    : SearchProviderBase(httpClient, options.Value.Altavista, logger)
{
    protected override HttpRequestMessage CreateRequest(string term)
    {
        var separator = Options.Endpoint.Contains('?', StringComparison.Ordinal) ? "&" : "?";
        var uri = $"{Options.Endpoint}{separator}query={Uri.EscapeDataString(term)}";

        return new HttpRequestMessage(HttpMethod.Get, uri);
    }
}
