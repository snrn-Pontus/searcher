using Searcher.Services;

namespace Searcher.Tests;

public sealed class SearchQueryParserTests
{
    private readonly SearchQueryParser _parser = new();

    [Fact]
    public void Parse_rejects_empty_input()
    {
        Assert.Throws<SearchValidationException>(() => _parser.Parse("   "));
    }

    [Fact]
    public void Parse_splits_on_any_whitespace()
    {
        var terms = _parser.Parse(" Hello\tworld\nagain ");

        Assert.Equal(new[] { "Hello", "world", "again" }, terms);
    }

    [Fact]
    public void Parse_preserves_repeated_words()
    {
        var terms = _parser.Parse("hello hello");

        Assert.Equal(new[] { "hello", "hello" }, terms);
    }

    [Fact]
    public void Parse_limits_number_of_terms()
    {
        var query = string.Join(' ', Enumerable.Repeat("word", SearchQueryParser.MaxTerms + 1));

        Assert.Throws<SearchValidationException>(() => _parser.Parse(query));
    }
}