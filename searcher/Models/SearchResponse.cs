namespace Searcher.Models;

public sealed record SearchResponse(
    string Query,
    IReadOnlyList<string> Terms,
    IReadOnlyList<SearchProviderSummary> Providers,
    long ElapsedMilliseconds);

public sealed record SearchProviderSummary(
    string Provider,
    long TotalHits,
    bool Succeeded,
    string? Error,
    IReadOnlyList<SearchTermResult> Terms);

public sealed record SearchTermResult(
    string Term,
    long? Hits,
    string? Error)
{
    public bool Succeeded => Error is null && Hits.HasValue;
    public bool FromCache { get; init; }
}