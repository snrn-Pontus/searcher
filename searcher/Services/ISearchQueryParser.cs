namespace Searcher.Services;

public interface ISearchQueryParser
{
    IReadOnlyList<string> Parse(string query);
}