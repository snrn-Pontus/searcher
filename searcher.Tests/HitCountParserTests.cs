using System.Text.Json;
using Searcher.Providers;

namespace Searcher.Tests;

public sealed class HitCountParserTests
{
    [Theory]
    [InlineData("123", 123)]
    [InlineData("{ \"hits\": 456 }", 456)]
    [InlineData("{ \"numberOfHits\": \"1,234\" }", 1234)]
    [InlineData("{ \"totalResults\": \"12 345\" }", 12345)]
    public void Parse_reads_supported_hit_count_shapes(string json, long expected)
    {
        using var document = JsonDocument.Parse(json);

        Assert.Equal(expected, HitCountParser.Parse(document.RootElement));
    }
}
