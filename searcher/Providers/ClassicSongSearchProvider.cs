using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Searcher.Options;

namespace Searcher.Providers;

public sealed class ClassicSongSearchProvider(
    HttpClient httpClient,
    IOptions<SearchEngineOptions> options,
    ILogger<ClassicSongSearchProvider> logger)
    : SearchProviderBase<ClassicSongSearchProvider.Response>(httpClient, options.Value.ClassicSong, logger)
{
    protected override HttpRequestMessage CreateRequest(string term) =>
        new(HttpMethod.Post, Options.Endpoint)
        {
            Content = JsonContent.Create(new { query = term })
        };

    protected override ProviderHitCountResult GetHitCount(Response response) =>
        new(response.TotalSearchHits, response.Errors ?? []);

    public sealed record Response(
        [property: JsonPropertyName("query")] string Query,
        [property: JsonPropertyName("totalSearchHits")]
        long TotalSearchHits,
        [property: JsonPropertyName("findHits")]
        IReadOnlyList<string> FindHits,
        [property: JsonPropertyName("errors")] IReadOnlyList<string>? Errors);
}
