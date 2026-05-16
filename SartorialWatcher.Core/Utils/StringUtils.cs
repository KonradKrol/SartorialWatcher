using System.Globalization;

namespace SartorialWatcher.Core.Utils;

public static class StringExtensions
{
    public static string ToTitleCaseInvariant(this string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value;

        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(
            value.ToLowerInvariant());
    }
    
    public static string ToSentenceCaseInvariant(this string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value;

        var lower = value.ToLowerInvariant();

        return char.ToUpperInvariant(lower[0]) + lower[1..];
    }
}