using System.Globalization;
using System.Text.RegularExpressions;
using AngleSharp.Html.Parser;
using SartorialWatcher.Core;

namespace SartorialWatcher.Scrapers;

// TODO: Add logs
public class WolczankaScraper(HttpClient http) : IScraper
{
    public async Task<ScraperResult> ScrapeAsync(ScrapingContext context)
    {
        var url = context.Url;
        var isOutlet = url.ToString().Contains("outlet");

        var html = await http.GetStringAsync(url); // TODO: cancellation token, headers
        var doc = await new HtmlParser().ParseDocumentAsync(html);

        var timestamp = DateTime.Now;

        var cards = doc.QuerySelectorAll(
            ".grid-row.product-page-items .product-page-item");

        var products = new List<ProductSnapshot>();
        foreach (var card in cards)
        {
            var id = card.GetAttribute("data-id") ?? throw new Exception("Id is null");
            var name = card.GetAttribute("data-item-name") ?? throw new Exception("Name is null");
            var currentPriceString = card.GetAttribute("data-price") ?? throw new Exception("Current price is null");
            var currentPrice = decimal.Parse(currentPriceString);

            var originalPriceString =
                card.QuerySelector(".product-price-omnibus > div:nth-child(2)")?.TextContent?.Trim();
            var omnibus30DaysPriceString = card.QuerySelector(
                ".product-price-omnibus > div:nth-child(1)")?.TextContent.Trim();

            var originalPrice = ParsePolishPrice(originalPriceString);
            var omnibus30DaysPrice = ParsePolishPrice(omnibus30DaysPriceString);

            var href = card.QuerySelector("a")?.GetAttribute("href") ?? throw new Exception("Href is null");

            var productSiteHtml = await http.GetStringAsync(href);
            var productDoc = await new HtmlParser().ParseDocumentAsync(productSiteHtml);

            var sizesSelector = productDoc.QuerySelectorAll(
                "#newSelectedSize .select-size-new__list__item");
            var sizes = new List<string>();
            foreach (var sizeElement in sizesSelector)
            {
                var size = sizeElement.GetAttribute("data-name");
                if (size is null)
                    continue;
                sizes.Add(size);
            }

            var tags = new List<string>();
            var materialContent = productDoc.QuerySelector("#collapse_material > div > span")?.TextContent.Trim();
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
            }

            string? imageUrl = null;

            var imagesSelector =
                productDoc.QuerySelectorAll(
                    ".desktop-gallery > div");

            var firstImage = imagesSelector.FirstOrDefault();
            if (firstImage is not null)
            {
                var htmlPicture = firstImage.QuerySelector(".desktop-gallery source");
                imageUrl = htmlPicture?.GetAttribute("srcset");
            }

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
                ImageUrl = imageUrl,
                Sizes = sizes.ToArray(),
                Tags = tags.ToArray(),
                Timestamp = timestamp,
                IsOutlet = isOutlet
            };

            products.Add(product);
        }

        return new ScraperResult { Products = products };
    }

    static decimal? ParsePolishPrice(string? text)
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