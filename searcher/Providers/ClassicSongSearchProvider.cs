using System.Net.Http.Json;
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
}
