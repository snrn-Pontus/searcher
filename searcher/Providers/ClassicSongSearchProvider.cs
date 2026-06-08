using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Searcher.Options;

namespace Searcher.Providers;

public sealed class ClassicSongSearchProvider(
    HttpClient httpClient,
    IOptions<SearchEngineOptions> options,
    ILogger<ClassicSongSearchProvider> logger)
    : SearchProviderBase(httpClient, options.Value.ClassicSong, logger)
{
    protected override HttpRequestMessage CreateRequest(string term) =>
        new(HttpMethod.Post, Options.Endpoint)
        {
            Content = JsonContent.Create(new { query = term })
        };

    protected override async Task<ProviderHitCountResult> ReadHitCountAsync(HttpContent content, CancellationToken cancellationToken)
    {
        var response = await content.ReadFromJsonAsync<ClassicSongSearchResponse>(cancellationToken);
        return response is null
            ? ProviderHitCountResult.Empty
            : new ProviderHitCountResult(response.TotalSearchHits, response.Errors ?? []);
    }

    private sealed record ClassicSongSearchResponse(
        [property: JsonPropertyName("query")] string Query,
        [property: JsonPropertyName("totalSearchHits")] long TotalSearchHits,
        [property: JsonPropertyName("findHits")] IReadOnlyList<string> FindHits,
        [property: JsonPropertyName("errors")] IReadOnlyList<string> Errors);
}
