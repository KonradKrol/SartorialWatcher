using Amazon.DynamoDBv2.Model;

namespace SartorialWatcher.Utils;

public static class DynamoDbUtils
{
    public static string? GetString(
        this Dictionary<string, AttributeValue> item,
        string key)
    {
        return item.TryGetValue(key, out var value)
            ? value.S
            : null;
    }
}