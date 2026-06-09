using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Searcher.Options;
using Searcher.Providers;

namespace Searcher.Tests;

public sealed class SearchProviderClientTests
{
    [Fact]
    public async Task Altavista_uses_get_query_string_token_header_and_typed_response()
    {
        HttpRequestMessage? captured = null;
        var provider = new AltavistaSearchProvider(
            new HttpClient(new StubHttpHandler(request =>
            {
                captured = request;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("""
                                                {
                                                  "query": "hello world",
                                                  "totalHits": 42,
                                                  "searchHits": ["result"],
                                                  "errors": []
                                                }
                                                """)
                });
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
    public async Task ClassicSong_uses_post_json_token_header_and_typed_response()
    {
        HttpRequestMessage? captured = null;
        string? capturedBody = null;
        var provider = new ClassicSongSearchProvider(
            new HttpClient(new StubHttpHandler(async request =>
            {
                captured = request;
                capturedBody = await request.Content!.ReadAsStringAsync();
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("""
                                                {
                                                  "query": "hello",
                                                  "totalSearchHits": 9,
                                                  "findHits": ["result"],
                                                  "errors": []
                                                }
                                                """)
                };
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

    [Fact]
    public async Task LibraryOfCongress_uses_get_without_token_and_typed_response()
    {
        HttpRequestMessage? captured = null;
        var provider = new LibraryOfCongressSearchProvider(
            new HttpClient(new StubHttpHandler(request =>
            {
                captured = request;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("""
                                                {
                                                  "pagination": {
                                                    "total": 3361
                                                  }
                                                }
                                                """)
                });
            })),
            Microsoft.Extensions.Options.Options.Create(CreateOptions()),
            NullLogger<LibraryOfCongressSearchProvider>.Instance);

        var result = await provider.SearchAsync("hey jude", CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(3361L, result.Hits.GetValueOrDefault());
        Assert.Equal(HttpMethod.Get, captured?.Method);
        Assert.Contains("q=hey%20jude", captured!.RequestUri!.Query);
        Assert.Contains("fo=json", captured.RequestUri.Query);
        Assert.Contains("at=pagination", captured.RequestUri.Query);
        Assert.False(captured.Headers.Contains("X-Api-Token"));
    }

    [Fact]
    public async Task Provider_errors_are_sanitized_in_term_result()
    {
        var provider = new AltavistaSearchProvider(
            new HttpClient(new StubHttpHandler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
                                            {
                                              "query": "hello",
                                              "totalHits": 0,
                                              "searchHits": [],
                                              "errors": ["internal provider detail"]
                                            }
                                            """)
            }))),
            Microsoft.Extensions.Options.Options.Create(CreateOptions()),
            NullLogger<AltavistaSearchProvider>.Instance);

        var result = await provider.SearchAsync("hello", CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal("Altavista could not complete the search.", result.Error);
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
        },
        LibraryOfCongress = new SearchProviderOptions
        {
            DisplayName = "Library of Congress",
            Endpoint = "https://example.test/loc"
        }
    };

    private sealed class StubHttpHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            handler(request);
    }
}
