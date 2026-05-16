using Amazon.DynamoDBv2.Model;

namespace SartorialWatcher.Core.Utils;

public static class DynamoDbUtils
{
    public static string? GetNullableString(
        this Dictionary<string, AttributeValue> item,
        string key)
    {
        return item.TryGetValue(key, out var value)
            ? value.S
            : null;
    }
}