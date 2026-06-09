namespace Searcher.Options;

public sealed class SearchEngineOptions
{
    public const string SectionName = "SearchEngines";

    public SearchProviderOptions Altavista { get; init; } = new()
    {
        DisplayName = "Altavista",
        Endpoint = "https://voyado-test-task-h8bshufyg8egejgb.northeurope-01.azurewebsites.net/api/AltavistaSearchEngine"
    };

    public SearchProviderOptions ClassicSong { get; init; } = new()
    {
        DisplayName = "Classic Song",
        Endpoint = "https://voyado-test-task-h8bshufyg8egejgb.northeurope-01.azurewebsites.net/api/ClassicSongSearchEngine"
    };

    public SearchProviderOptions LibraryOfCongress { get; init; } = new()
    {
        DisplayName = "Library of Congress",
        Endpoint = "https://www.loc.gov/search/"
    };

    public int CacheDurationSeconds { get; init; } = 300;
    public int MaxConcurrentRequestsPerProvider { get; init; } = 8;

    public static bool IsValid(SearchEngineOptions options) =>
        IsAbsoluteUrl(options.Altavista.Endpoint) &&
        IsAbsoluteUrl(options.ClassicSong.Endpoint) &&
        IsAbsoluteUrl(options.LibraryOfCongress.Endpoint) &&
        options.CacheDurationSeconds >= 0 &&
        options.MaxConcurrentRequestsPerProvider > 0;

    private static bool IsAbsoluteUrl(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}

public sealed class SearchProviderOptions
{
    public string DisplayName { get; init; } = string.Empty;
    public string Endpoint { get; init; } = string.Empty;
    public string ApiToken { get; init; } = string.Empty;
    public int TimeoutSeconds { get; init; } = 10;
}
