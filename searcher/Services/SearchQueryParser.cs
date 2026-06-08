using System.Text.RegularExpressions;

namespace Searcher.Services;

public sealed partial class SearchQueryParser : ISearchQueryParser
{
    public const int MaxQueryLength = 500;
    public const int MaxTerms = 25;

    public IReadOnlyList<string> Parse(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new SearchValidationException("Enter one or more words to search for.");
        }

        var trimmed = query.Trim();
        if (trimmed.Length > MaxQueryLength)
        {
            throw new SearchValidationException($"Search query can be at most {MaxQueryLength} characters.");
        }

        var terms = WhitespaceRegex().Split(trimmed).Where(term => term.Length > 0).ToArray();
        if (terms.Length > MaxTerms)
        {
            throw new SearchValidationException($"Search query can contain at most {MaxTerms} words.");
        }

        return terms;
    }

    [GeneratedRegex("\\s+", RegexOptions.Compiled)]
    private static partial Regex WhitespaceRegex();
}
