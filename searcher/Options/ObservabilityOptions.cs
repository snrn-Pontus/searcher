namespace Searcher.Options;

public sealed class ObservabilityOptions
{
    public const string SectionName = "Observability";

    public bool DetailedSearchLogging { get; init; } = true;
}