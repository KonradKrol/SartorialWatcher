using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SartorialWatcher.Core.Domain;

namespace SartorialWatcher.Core.Infrastructure.ReportsHistory;

public class DynamoReportsHistory(
    IAmazonDynamoDB dynamoDb,
    IConfiguration configuration,
    ILogger<DynamoReportsHistory> logger) : IReportsHistory
{
    private string TableName => configuration["Aws:Dynamo:TableName"] ??
                                throw new Exception("Aws DynamoDB table name is missing");

    public async Task RegisterNewReportAsync(Report report)
    {
        logger.LogInformation("Requested to register new report at {DateTimeOffset}", report.Timestamp);
        var putItemRequest = new PutItemRequest()
        {
            TableName = TableName,
            Item = CreateDynamoItem(report),
            ConditionExpression = "attribute_not_exists(SK)"
        };
        await dynamoDb.PutItemAsync(putItemRequest);
    }

    private Dictionary<string, AttributeValue> CreateDynamoItem(Report report)
    {
        return new Dictionary<string, AttributeValue>()
        {
            ["PK"] = new("REPORTS"),
            ["SK"] = new(report.Timestamp.ToString("O"))
        };
    }


    public async Task<DateTimeOffset?> GetLatestReportDateAsync()
    {
        var request = new QueryRequest()
        {
            TableName = TableName,
            KeyConditionExpression = "PK = :pk",
            ExpressionAttributeValues = new()
            {
                [":pk"] = new AttributeValue($"REPORTS")
            },
            ScanIndexForward = false,
            Limit = 1,
        };
        var response = await dynamoDb.QueryAsync(request);
        var lastDynamoItem = response.Items.FirstOrDefault();
        if (lastDynamoItem is null) return null;
        return CreateReport(lastDynamoItem).Timestamp;
    }

    private Report CreateReport(Dictionary<string, AttributeValue> dynamoItem)
    {
        return new Report()
        {
            Timestamp = DateTimeOffset.Parse(dynamoItem["SK"].S)
        };
    }
}