using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Searcher.Options;
using Searcher.Providers;

namespace Searcher.Tests;

public sealed class SearchProviderClientTests
{
    [Fact]
    public async Task Altavista_uses_get_query_string_and_token_header()
    {
        HttpRequestMessage? captured = null;
        var provider = new AltavistaSearchProvider(
            new HttpClient(new StubHttpHandler(request =>
            {
                captured = request;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{ \"hits\": 42 }") });
            })),
            Microsoft.Extensions.Options.Options.Create(CreateOptions()),
            NullLogger<AltavistaSearchProvider>.Instance);

        var result = await provider.SearchAsync("hello world", CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(42L, result.Hits.GetValueOrDefault());
        Assert.Equal(HttpMethod.Get, captured?.Method);
        Assert.Contains("query=hello%20world", captured!.RequestUri!.Query);
        Assert.Equal("alt-token", captured.Headers.GetValues("X-Api-Token").Single());
    }

    [Fact]
    public async Task ClassicSong_uses_post_json_and_token_header()
    {
        HttpRequestMessage? captured = null;
        string? capturedBody = null;
        var provider = new ClassicSongSearchProvider(
            new HttpClient(new StubHttpHandler(async request =>
            {
                captured = request;
                capturedBody = await request.Content!.ReadAsStringAsync();
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{ \"count\": 9 }") };
            })),
            Microsoft.Extensions.Options.Options.Create(CreateOptions()),
            NullLogger<ClassicSongSearchProvider>.Instance);

        var result = await provider.SearchAsync("hello", CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(9L, result.Hits.GetValueOrDefault());
        Assert.Equal(HttpMethod.Post, captured?.Method);
        Assert.Equal("classic-token", captured?.Headers.GetValues("X-Api-Token").Single());
        Assert.Contains("\"query\":\"hello\"", capturedBody);
    }

    private static SearchEngineOptions CreateOptions() => new()
    {
        Altavista = new SearchProviderOptions
        {
            DisplayName = "Altavista",
            Endpoint = "https://example.test/altavista",
            ApiToken = "alt-token"
        },
        ClassicSong = new SearchProviderOptions
        {
            DisplayName = "Classic Song",
            Endpoint = "https://example.test/classic",
            ApiToken = "classic-token"
        }
    };

    private sealed class StubHttpHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            handler(request);
    }
}
