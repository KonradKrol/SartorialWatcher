using System.Globalization;
using System.Text.RegularExpressions;
using AngleSharp.Html.Parser;
using SartorialWatcher.Core.Core;

namespace SartorialWatcher.Core.Scrapers;

public class BytomScraper(HttpClient http) : IScraper
{
    public async Task<ScraperResult> ScrapeAsync(ScrapingContext context)
    {
        var url = context.Url;
        var isOutlet = url.ToString().Contains("outlet");

        var html = await http.GetStringAsync(url); // TODO: cancellation token, headers
        var doc = await new HtmlParser().ParseDocumentAsync(html);

        var timestamp = DateTime.Now;

        var cards = doc.QuerySelectorAll(
            ".product-page-items > .product-page-item");

        var products = new List<ProductSnapshot>();
        
        foreach (var card in cards)
        {
            var id = card.GetAttribute("data-id") ?? throw new Exception("Id is null");
            var name = card.GetAttribute("data-item-name") ?? throw new Exception("Name is null");
            var currentPriceString = card.GetAttribute("data-price") ?? throw new Exception("Current price is null");
            var currentPrice = decimal.Parse(currentPriceString);

            var originalPriceString =
                card.QuerySelector(".is_product-price-omnibus > div:nth-child(2)")?.TextContent?.Trim();
            var omnibus30DaysPriceString = card.QuerySelector(
                ".is_product-price-omnibus > div:nth-child(1)")?.TextContent.Trim();

            var originalPrice = ParsePolishPrice(originalPriceString);
            var omnibus30DaysPrice = ParsePolishPrice(omnibus30DaysPriceString);

            var href = card.QuerySelector("a")?.GetAttribute("href") ?? throw new Exception("Href is null");

            var productSiteHtml = await http.GetStringAsync(href);
            var productDoc = await new HtmlParser().ParseDocumentAsync(productSiteHtml);

            var sizesSelector = productDoc.QuerySelectorAll(
                "#product-variant-form > select > option");
            var sizes = new List<string>(); // TODO
            foreach (var sizeElement in sizesSelector)
            {
                var size = sizeElement.TextContent.Trim();
                if (size.Contains("Wybierz")) continue;
                if (sizeElement.GetAttribute("data-quantity") == "0")
                {
                    continue;
                }

                sizes.Add(size);
            }

            var tags = new List<string>();
            var materialContent = productDoc.QuerySelector("#collapse_material > div > p")?.TextContent.Trim();
            if (materialContent is not null)
            {
                if (materialContent.Contains("Bawełna") || materialContent.Contains("bawełna") ||
                    materialContent.Contains("100% Bawełna"))
                {
                    tags.Add("Bawełna");
                }

                if (materialContent.Contains("Len") || materialContent.Contains("len") ||
                    materialContent.Contains("lniana") || materialContent.Contains("100% Len"))
                {
                    tags.Add("Len");
                }

                if (materialContent.Contains("organiczna") || materialContent.Contains("Organiczna"))
                {
                    tags.Add("Organiczna");
                }

                if (materialContent.Contains("merceryzowana") || materialContent.Contains("Merceryzowana"))
                {
                    tags.Add("Merceryzowana");
                }

                if (materialContent.Contains("Egipska") || materialContent.Contains("egipska") ||
                    materialContent.Contains("Egipt"))
                {
                    tags.Add("Egipska");
                }

                if (materialContent.Contains("Two ply") || materialContent.Contains("two ply") ||
                    materialContent.Contains("dwuskrętnej") || materialContent.Contains("dwuskrętna") ||
                    materialContent.Contains("Dwuskrętnej") || materialContent.Contains("Dwuskrętna"))
                {
                    tags.Add("Two ply");
                }

                if (materialContent.Contains("wełna") || materialContent.Contains("Wełna"))
                {
                    tags.Add("Wełna");
                }
            }
            //
            // string? imageUrl = null;
            //
            // var imagesSelector =
            //     productDoc.QuerySelectorAll(
            //         ".row.desktop-part-gallery > div:nth-child(1) > a > picture > img");
            //
            //
            // var firstImage = imagesSelector.FirstOrDefault();
            // if (firstImage is not null)
            // {
            //     var htmlPicture = firstImage.QuerySelector(".desktop-gallery source");
            //     imageUrl = htmlPicture?.GetAttribute("srcset");
            // }

            var description = productDoc.QuerySelector("#collapse_description > div > p:nth-child(1)")?.TextContent?
                .Trim();

            var product = new ProductSnapshot
            {
                Id = id,
                Name = name,
                Description = description,
                Brand = "Wólczanka",
                Url = href,
                CurrentPrice = currentPrice,
                OriginalPrice = originalPrice ?? currentPrice,
                Omnibus30DaysPrice = omnibus30DaysPrice ?? currentPrice,
                // ImageUrl = imageUrl,
                Sizes = sizes.ToArray(),
                Tags = tags.ToArray(),
                Timestamp = timestamp,
                IsOutlet = isOutlet
            };

            products.Add(product);
        }

        return new ScraperResult { Products = products };
    }

    private static decimal? ParsePolishPrice(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var match = Regex.Match(text, @"\d+,\d{2}");

        if (!match.Success)
            return null;

        return decimal.Parse(
            match.Value.Replace(',', '.'),
            CultureInfo.InvariantCulture);
    }
}