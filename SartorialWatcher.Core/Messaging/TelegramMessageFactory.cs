using Microsoft.Extensions.Logging;
using SartorialWatcher.Core.Core;
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
            newProducts.Where(EligibleToCottonNewDeal).OrderByPrice().Take(30).ToList();
        var newLinenDeals = newProducts.Where(EligibleToLinenNewDeal).OrderByPrice().Take(40).ToList();
        var otherDeals = oldProducts.Where(EligibleToOtherDeal).OrderByPrice().Take(5)
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
        if (product.Brand == "Bytom")
        {
            return product.Tags.Contains("Bawełna") && product is { CurrentPrice: < 200m, Discount: >= 0.3m } && product.Sizes.Contains("188-194/40") || product.Sizes.Contains("188-194/39");
        }
        else
        {
            return product.Tags.Contains("Bawełna") && product is { CurrentPrice: < 120m, Discount: >= 0.3m } && product.Sizes.Contains("188-194/40") || product.Sizes.Contains("188-194/39");
        }
    }

    private static bool EligibleToLinenNewDeal(ProductSnapshot product)
    {
        if (product.Brand == "Bytom")
        {
            return product.Tags.Contains("Len") && product.Discount >= 0.15m;
        }
        else
        {
            return product.Tags.Contains("Len") && product.Discount >= 0.3m;
        }
    }

    private static bool EligibleToOtherDeal(ProductSnapshot product)
    {
        return product.Discount > 0;
    }
}