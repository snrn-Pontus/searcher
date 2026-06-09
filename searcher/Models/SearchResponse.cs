namespace Searcher.Models;

public sealed class SearchResponse
{
    public required string Query { get; init; }
    public required IReadOnlyList<string> Terms { get; init; }
    public required IReadOnlyList<SearchProviderSummary> Providers { get; init; }
    public required long ElapsedMilliseconds { get; init; }
}

public sealed class SearchProviderSummary
{
    public required string Provider { get; init; }
    public required long TotalHits { get; init; }
    public required bool Succeeded { get; init; }
    public required string? Error { get; init; }
    public required IReadOnlyList<SearchTermResult> Terms { get; init; }
}

public sealed record SearchTermResult
{
    public required string Term { get; init; }
    public required long? Hits { get; init; }
    public required string? Error { get; init; }
    public bool Succeeded => Error is null && Hits.HasValue;
    public bool FromCache { get; init; }
}
