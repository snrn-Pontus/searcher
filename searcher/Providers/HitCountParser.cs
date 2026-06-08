using System.Globalization;
using System.Text.Json;

namespace Searcher.Providers;

public static class HitCountParser
{
    private static readonly string[] PreferredPropertyNames =
    [
        "hits", "hitCount", "totalHits", "total", "count", "resultCount", "totalResults", "numberOfHits", "results"
    ];

    public static long? Parse(JsonElement element)
    {
        if (TryReadNumber(element, out var directValue))
        {
            return directValue;
        }

        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        foreach (var propertyName in PreferredPropertyNames)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase) &&
                    TryReadNumber(property.Value, out var propertyValue))
                {
                    return propertyValue;
                }
            }
        }

        foreach (var property in element.EnumerateObject())
        {
            if (TryReadNumber(property.Value, out var fallbackValue))
            {
                return fallbackValue;
            }
        }

        return null;
    }

    private static bool TryReadNumber(JsonElement element, out long value)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Number:
                return element.TryGetInt64(out value);
            case JsonValueKind.String:
                var normalized = element.GetString()?.Replace(" ", string.Empty, StringComparison.Ordinal).Replace(",", string.Empty, StringComparison.Ordinal);
                return long.TryParse(normalized, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
            default:
                value = 0;
                return false;
        }
    }
}
