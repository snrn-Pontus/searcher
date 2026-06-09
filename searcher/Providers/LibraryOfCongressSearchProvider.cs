using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Searcher.Options;

namespace Searcher.Providers;

public sealed class LibraryOfCongressSearchProvider(
    HttpClient httpClient,
    IOptions<SearchEngineOptions> options,
    ILogger<LibraryOfCongressSearchProvider> logger)
    : SearchProviderBase<LibraryOfCongressSearchProvider.Response>(httpClient, options.Value.LibraryOfCongress, logger)
{
    protected override bool RequiresApiToken => false;

    protected override HttpRequestMessage CreateRequest(string term)
    {
        var parameters = new Dictionary<string, string>
        {
            ["q"] = term,
            ["fo"] = "json",
            ["at"] = "pagination"
        };
        var query = string.Join('&', parameters.Select(parameter =>
            $"{Uri.EscapeDataString(parameter.Key)}={Uri.EscapeDataString(parameter.Value)}"));
        var separator = Options.Endpoint.Contains('?', StringComparison.Ordinal) ? "&" : "?";

        return new HttpRequestMessage(HttpMethod.Get, $"{Options.Endpoint}{separator}{query}");
    }

    protected override ProviderHitCountResult GetHitCount(Response response) =>
        new(response.Pagination?.Total, []);

    public sealed record Response(
        [property: JsonPropertyName("pagination")]
        Pagination? Pagination);

    public sealed record Pagination(
        [property: JsonPropertyName("total")]
        long Total);
}
