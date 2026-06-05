using Microsoft.Extensions.Logging;
using SartorialWatcher.Core.Domain;
using SartorialWatcher.Core.Utils;

namespace SartorialWatcher.Core.Messaging;

public class TelegramMessageFactory(ILogger<TelegramMessageFactory> logger)
    : IReportMessageFactory
{
    public async Task<string> CreateMessage(ReportMessageContext context)
    {
        logger.LogInformation("Started creating a Telegram message content");
        var newProducts = context.ProductsAddedSinceLastReport;

        var newDealIds = context.ProductsAddedSinceLastReport.Select(product => product.Id).ToList();
        var oldProducts = context.Products.Where(product => !newDealIds.Contains(product.Id)).ToList();

        var newCottonDeals =
            newProducts.Where(EligibleToCottonNewDeal).OrderByPrice().Take(50).ToList();
        var newLinenDeals = newProducts.Where(EligibleToLinenNewDeal).OrderByPrice().Take(50).ToList();
        var otherDeals = oldProducts.Where(EligibleToOtherDeal).OrderByPrice().Take(15)
            .ToList();
        logger.LogInformation("Got {CottonCount} cotton deals, {LinenCount} linen deals and {OtherCount} other deals",
            newCottonDeals.Count, newLinenDeals.Count, otherDeals.Count);

        return CreateStringMessage(newCottonDeals, newLinenDeals, otherDeals);
    }

    private static string CreateStringMessage(List<ProductSnapshot> newCottonDeals, List<ProductSnapshot> newLinenDeals,
        List<ProductSnapshot> otherDeals)
    {
        var newCottonDealsSegments = newCottonDeals.Select(CreateProductString);
        var newLinenDealsSegments = newLinenDeals.Select(CreateProductString);
        var otherDealsSegments = otherDeals.Select(CreateProductString);

        var message = "";

        if (newCottonDeals.Count > 0)
        {
            message += $"""
                        <b>Nowe bawełniane koszule:</b>

                        {string.Join("\n", newCottonDealsSegments)}
                        """;
        }

        if (newLinenDeals.Count > 0)
        {
            message += $"""


                        <b>Nowe lniane koszule:</b>

                        {string.Join("\n", newLinenDealsSegments)}
                        """;
        }

        if (newCottonDeals.Count == 0 && newLinenDeals.Count == 0)
        {
            return "";
        }

        if (otherDeals.Count > 0)
        {
            message += $"""


                        <b>Nie przegap także:</b>

                        {string.Join("\n", otherDealsSegments)}
                        """;
        }

        return message;

        string CreateProductString(ProductSnapshot product, int index)
        {
            string? material = null;
            if (product.Tags.Contains("Len"))
            {
                material = "LEN";
            }
            else if (product.Tags.Contains("Bawełna"))
            {
                material = null;
            }

            var productString = $"{index + 1}. {product.Name}, {product.CurrentPrice}zł";
            if (product.Discount > 0)
            {
                productString += $" <i>(-{(int)(product.Discount * 100)}%)</i>";
            }

            if (product.Omnibus30DaysDiscount > 0)
            {
                productString += $" <i>(-{(int)(product.Omnibus30DaysDiscount * 100)}% last 30d)</i>";
            }

            if (material is { } materialString)
            {
                productString += $" {materialString}";
            }

            if (!string.IsNullOrWhiteSpace(product.Url))
            {
                productString += $" --> <a href=\"{product.Url}\">Sprawdź</a>";
            }

            return productString;
        }
    }

    private static bool EligibleToCottonNewDeal(ProductSnapshot product)
    {
        return product.Brand switch
        {
            "Bytom" =>
                product.Tags.Contains("Bawełna") &&
                product is { CurrentPrice: < 200m, Discount: >= 0.25m },

            "Vistula" =>
                product.Tags.Contains("Bawełna") &&
                product is { CurrentPrice: < 150m, Discount: >= 0.25m },

            "Wólczanka" =>
                product.Tags.Contains("Bawełna") &&
                product is { CurrentPrice: < 150m, Discount: >= 0.25m },
            _ => true,
        };
    }

    private static bool EligibleToLinenNewDeal(ProductSnapshot product)
    {
        return product.Brand switch
        {
            "Bytom" =>
                product.Tags.Contains("Len") &&
                product is { Discount: >= 0.1m },

            "Vistula" =>
                product.Tags.Contains("Len") &&
                product is { Discount: >= 0.1m },

            "Wólczanka" =>
                product.Tags.Contains("Len") &&
                product is { Discount: >= 0.1m },
            _ => true,
        };
    }

    private static bool EligibleToOtherDeal(ProductSnapshot product)
    {
        return product.Discount > 0;
    }
}