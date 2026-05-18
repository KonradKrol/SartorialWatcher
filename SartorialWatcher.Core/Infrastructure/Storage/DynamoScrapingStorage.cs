using System.Globalization;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SartorialWatcher.Core.Domain;
using SartorialWatcher.Core.Utils;

namespace SartorialWatcher.Core.Infrastructure.Storage;

public class DynamoScrapingStorage(
    IAmazonDynamoDB dynamoDb,
    IConfiguration configuration,
    ILogger<DynamoScrapingStorage> logger) : IScrapingStorage
{
    private string TableName => configuration["Aws:Dynamo:TableName"] ??
                                throw new Exception("Aws DynamoDB table name is missing");

    public async Task AddAsync(List<ProductSnapshot> productSnapshots)
    {
        logger.LogInformation("Saving {ProductsCount} products", productSnapshots.Count);
        var transactionWriteItemsByProduct = productSnapshots.DistinctBy(product => product.Id).ToDictionary(
            product => product.Id,
            product =>
            {
                var productInCurrentPartition =
                    ProductToCurrentPartitionWriteRequest(product);

                var productInHistoryPartition =
                    ProductToHistoryPartitionWriteRequest(product);

                return new List<TransactWriteItem>
                {
                    new()
                    {
                        Put = new Put
                        {
                            TableName = TableName,
                            Item = productInCurrentPartition,

                            ConditionExpression =
                                "(attribute_not_exists(#timestamp)) OR " +
                                "(#timestamp < :newTimestamp " +
                                "AND #currentPrice <> :newCurrentPrice)",

                            ExpressionAttributeNames = new()
                            {
                                ["#timestamp"] = "Timestamp",
                                ["#currentPrice"] = "CurrentPrice"
                            },

                            ExpressionAttributeValues = new()
                            {
                                [":newTimestamp"] = new()
                                {
                                    S = product.Timestamp.ToString("O")
                                },

                                [":newCurrentPrice"] = new()
                                {
                                    N = product.CurrentPrice
                                        .ToString(CultureInfo.InvariantCulture)
                                }
                            }
                        }
                    },

                    new()
                    {
                        Put = new Put
                        {
                            TableName = TableName,
                            Item = productInHistoryPartition,
                        }
                    },
                };
            });

        var failedTransactions = 0;
        decimal consumedRcu = 0;
        decimal consumedWcu = 0;

        foreach (var productId in productSnapshots.Select(product => product.Id))
        {
            var transactWriteItems = transactionWriteItemsByProduct[productId];

            var transactionRequest = new TransactWriteItemsRequest
            {
                ReturnConsumedCapacity = ReturnConsumedCapacity.TOTAL,
                TransactItems = transactWriteItems
            };

            try
            {
                var transactionResponse = await dynamoDb.TransactWriteItemsAsync(transactionRequest);

                foreach (var consumedCapacity in transactionResponse.ConsumedCapacity)
                {
                    consumedRcu += (decimal?)consumedCapacity.ReadCapacityUnits ?? 0;
                    consumedWcu += (decimal?)consumedCapacity.WriteCapacityUnits ?? 0;
                }
            }
            catch (TransactionCanceledException transactionCanceledException)
            {
                logger.LogDebug("Transaction failed for product {ProductId} because ConditionalCheck has failed.",
                    productId);
                failedTransactions++;
            }
        }

        var averageConsumedRcu = (double)consumedRcu / transactionWriteItemsByProduct.Count / 2;
        var averageConsumedWcu = (double)consumedWcu / transactionWriteItemsByProduct.Count / 2;

        logger.LogInformation(
            "Transact-written {ProductsCount} consuming {ConsumedRcu} RCUs and {ConsumedWcu} WCUs. In average, consumed {AverageRcu} RCUs and {AverageWcu} WCUs per transaction. {FailedTransactionsCount} transactions failed",
            productSnapshots.Count - failedTransactions, consumedRcu, consumedWcu, averageConsumedRcu.RoundTo(2),
            averageConsumedWcu.RoundTo(2), failedTransactions);
    }

    private static Dictionary<string, AttributeValue>
        ProductToCurrentPartitionWriteRequest(ProductSnapshot product)
    {
        var attributes = CreateCommonAttributes(product);

        attributes["PK"] = new("CURRENT");
        attributes["SK"] = new($"PRODUCT#{product.Id}");
        attributes["Timestamp"] = new(product.Timestamp.ToString("O"));

        return attributes;
    }

    private static Dictionary<string, AttributeValue>
        ProductToHistoryPartitionWriteRequest(ProductSnapshot product)
    {
        var attributes = CreateCommonAttributes(product);

        attributes["PK"] = new($"PRODUCT#{product.Id}");
        attributes["SK"] = new(product.Timestamp.ToString("O"));

        return attributes;
    }

    private static Dictionary<string, AttributeValue> CreateCommonAttributes(ProductSnapshot product)
    {
        var attributes = new Dictionary<string, AttributeValue>
        {
            ["Brand"] = new(product.Brand),
            ["Name"] = new(product.Name),
            ["Url"] = new(product.Url),
            ["CurrentPrice"] = new()
            {
                N = product.CurrentPrice.ToString(CultureInfo.InvariantCulture)
            },

            ["OriginalPrice"] = new()
            {
                N = product.OriginalPrice.ToString(CultureInfo.InvariantCulture)
            },

            ["Omnibus30DaysPrice"] = new()
            {
                N = product.Omnibus30DaysPrice.ToString(CultureInfo.InvariantCulture)
            },

            ["IsOutlet"] = new()
            {
                BOOL = product.IsOutlet
            },
        };

        if (product.Description is { } description)
        {
            attributes["Description"] = new AttributeValue(description);
        }

        if (product.ImageUrl is { } imageUrl)
        {
            attributes["ImageUrl"] = new AttributeValue(imageUrl);
        }

        if (product.Sizes.Length > 0)
        {
            attributes["Sizes"] = new AttributeValue { SS = product.Sizes.ToList() };
        }

        if (product.Tags.Length > 0)
        {
            attributes["Tags"] = new AttributeValue { SS = product.Tags.ToList() };
        }

        return attributes;
    }

    // TODO: Tutaj potrzeba GSI
    public async Task<IReadOnlyCollection<ProductSnapshot>> GetCurrentSnapshotsSinceAsync(DateTimeOffset dateTime)
    {
        var queryRequest = new QueryRequest()
        {
            TableName = TableName,
            KeyConditionExpression = "PK = :pk",
            ExpressionAttributeValues = new()
            {
                [":pk"] = new AttributeValue($"CURRENT"),
                [":ts"] = new AttributeValue(dateTime.ToString("O"))
            },
            FilterExpression = "Timestamp >= :ts",
        };
        var response = await dynamoDb.QueryAsync(queryRequest);

        var responseItems = response.Items;
        var productSnapshots = responseItems.Select(CreateSnapshotFromDynamoItem).ToList();
        return productSnapshots;
    }

    public async Task<ProductSnapshot?> GetLatestSnapshotAsync(string productId)
    {
        var queryRequest = new QueryRequest()
        {
            TableName = TableName,
            KeyConditionExpression = "PK = :pk",
            ExpressionAttributeValues = new()
            {
                [":pk"] = new AttributeValue($"PRODUCT#{productId}")
            },
            ScanIndexForward = false,
            Limit = 1,
        };
        var response = await dynamoDb.QueryAsync(queryRequest);

        var responseItems = response.Items;
        var productSnapshots = responseItems.Select(CreateSnapshotFromDynamoItem).ToList();
        return productSnapshots.SingleOrDefault();
    }

    public async Task<IReadOnlyCollection<ProductSnapshot>> GetCurrentProductsAsync()
    {
        var queryRequest = new QueryRequest()
        {
            TableName = TableName,
            KeyConditionExpression = "PK = :pk",
            ExpressionAttributeValues = new()
            {
                [":pk"] = new AttributeValue($"CURRENT")
            }
        };
        var response = await dynamoDb.QueryAsync(queryRequest);
        var responseItems = response.Items;
        var productSnapshots = responseItems.Select(CreateSnapshotFromDynamoItem).ToList();
        return productSnapshots;
    }

    public async Task<ProductSnapshot?> FindCheapestSnapshotAsync(string productId)
    {
        var queryRequest = new QueryRequest()
        {
            TableName = TableName,
            KeyConditionExpression = "PK = :pk",
            ExpressionAttributeValues = new()
            {
                [":pk"] = new AttributeValue($"PRODUCT#{productId}")
            }
        };
        var response = await dynamoDb.QueryAsync(queryRequest);

        var responseItems = response.Items;
        var productSnapshots = responseItems.Select(CreateSnapshotFromDynamoItem).ToList();
        return productSnapshots.MinBy(product => product.CurrentPrice);
    }


    private static ProductSnapshot CreateSnapshotFromDynamoItem(Dictionary<string, AttributeValue> dynamoItem)
    {
        var pk = dynamoItem["PK"].S;
        var sk = dynamoItem["SK"].S;

        string id, timestamp;

        if (pk == "CURRENT")
        {
            id = sk["PRODUCT#".Length..]; // SK = PRODUCT#123
            timestamp = dynamoItem["Timestamp"].S;
        }
        else if (pk.StartsWith("PRODUCT#"))
        {
            id = pk["PRODUCT#".Length..]; // PK = PRODUCT#123
            timestamp = sk["".Length..];
        }
        else
        {
            throw new Exception($"Invalid PK or SK format (PK={pk}, SK={sk})");
        }

        return new ProductSnapshot
        {
            Id = id,
            Brand = dynamoItem["Brand"].S,
            Name = dynamoItem["Name"].S,
            Description = dynamoItem.GetNullableString("Description"),
            Url = dynamoItem["Url"].S,
            ImageUrl = dynamoItem.GetNullableString("ImageUrl"),
            Sizes = dynamoItem.GetValueOrDefault("Sizes")?.SS.ToArray() ?? [],
            Tags = dynamoItem.GetValueOrDefault("Tags")?.SS.ToArray() ?? [],
            CurrentPrice = decimal.Parse(dynamoItem["CurrentPrice"].N),
            OriginalPrice = decimal.Parse(dynamoItem["OriginalPrice"].N),
            Omnibus30DaysPrice = decimal.Parse(dynamoItem["Omnibus30DaysPrice"].N),
            Timestamp = DateTimeOffset.Parse(timestamp),
            IsOutlet = dynamoItem["IsOutlet"].BOOL ?? throw new Exception("Cannot parse IsOutlet"),
        };
    }
}