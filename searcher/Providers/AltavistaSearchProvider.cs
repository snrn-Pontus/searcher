using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Searcher.Options;

namespace Searcher.Providers;

public sealed class AltavistaSearchProvider(
    HttpClient httpClient,
    IOptions<SearchEngineOptions> options,
    ILogger<AltavistaSearchProvider> logger)
    : SearchProviderBase<AltavistaSearchProvider.Response>(httpClient, options.Value.Altavista, logger)
{
    protected override HttpRequestMessage CreateRequest(string term)
    {
        var separator = Options.Endpoint.Contains('?', StringComparison.Ordinal) ? "&" : "?";
        var uri = $"{Options.Endpoint}{separator}query={Uri.EscapeDataString(term)}";

        return new HttpRequestMessage(HttpMethod.Get, uri);
    }

    protected override ProviderHitCountResult GetHitCount(Response response) =>
        new(response.TotalHits, response.Errors ?? []);

    public sealed record Response(
        [property: JsonPropertyName("query")] string Query,
        [property: JsonPropertyName("totalHits")]
        long TotalHits,
        [property: JsonPropertyName("searchHits")]
        IReadOnlyList<string> SearchHits,
        [property: JsonPropertyName("errors")] IReadOnlyList<string>? Errors);
}
