using System.Globalization;
using System.Text.RegularExpressions;
using AngleSharp.Html.Parser;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using SartorialWatcher.Core.Domain;
using SartorialWatcher.Core.Utils;

namespace SartorialWatcher.Core.Scrapers;

public class BytomScraper(IHttpClientFactory httpFactory, ILogger<BytomScraper> logger) : IScraper
{
    private readonly HttpClient _http = httpFactory.CreateClient("scraper");

    public async Task<ScraperResult> ScrapeAsync(ScrapingContext context, CancellationToken cancellationToken)
    {
        var baseUrl = context.Url;
        var isOutlet = baseUrl.ToString().Contains("outlet");

        var products = new List<ProductSnapshot>();
        var page = 0;

        using (logger.BeginScope(new Dictionary<string, object>
               {
                   ["BaseUrl"] = baseUrl
               }))
        {
            while (true)
            {
                page++;
                using (logger.BeginScope(new Dictionary<string, object>
                           {
                               ["Page"] = page
                           }
                       ))
                {
                    logger.LogInformation("Scraping a page");
                    var pageProducts = await ScrapePage(baseUrl, isOutlet, page, cancellationToken);
                    logger.LogInformation("Scrapped {ProductsCount} products", pageProducts.Count);
                    if (pageProducts.Count == 0)
                    {
                        break;
                    }

                    products.AddRange(pageProducts);
                }
            }

            logger.LogInformation("Scraped totally {ProductsCount} products at {PagesCount} pages", products.Count,
                page);

            return new ScraperResult { Products = products, PagesCount = page };
        }
    }

    private async Task<List<ProductSnapshot>> ScrapePage(Uri baseUrl, bool isOutlet, int page, CancellationToken cancellationToken)
    {
        var finalUrl = QueryHelpers.AddQueryString(baseUrl.ToString(), "page", page.ToString());
        var html = await _http.GetStringAsync(finalUrl, cancellationToken);
        var doc = await new HtmlParser().ParseDocumentAsync(html);

        var timestamp = DateTime.Now;

        var cards = doc.QuerySelectorAll(
            ".product-page-items > .product-page-item");

        if (cards.Count == 0) return [];

        var products = new List<ProductSnapshot>();

        foreach (var card in cards)
        {
            var id = card.GetAttribute("data-id") ?? throw new Exception("Id is null");
            var name = card.GetAttribute("data-item-name")?.ToSentenceCaseInvariant() ??
                       throw new Exception("Name is null");
            var currentPriceString =
                card.GetAttribute("data-price") ?? throw new Exception("Current price is null");
            var currentPrice = decimal.Parse(currentPriceString);

            var originalPriceString =
                card.QuerySelector(".is_product-price-omnibus > div:nth-child(2)")?.TextContent?.Trim();
            var omnibus30DaysPriceString = card.QuerySelector(
                ".is_product-price-omnibus > div:nth-child(1)")?.TextContent.Trim();

            var originalPrice = ParsePolishPrice(originalPriceString);
            var omnibus30DaysPrice = ParsePolishPrice(omnibus30DaysPriceString);

            var href = card.QuerySelector("a")?.GetAttribute("href") ?? throw new Exception("Href is null");

            var productSiteHtml = await _http.GetStringAsync(href, cancellationToken);
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
            var materialContent = productDoc.QuerySelector("#collapse_description > div > p:nth-child(3)")
                ?.TextContent
                .Trim();
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

                if (materialContent.Contains("Poliamid") || materialContent.Contains("poliamid"))
                {
                    tags.Add("Poliamid");
                }

                if (materialContent.Contains("Elastan") || materialContent.Contains("elastan"))
                {
                    tags.Add("Elastan");
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

                if (Regex.IsMatch(materialContent, @"\beasy care\b", RegexOptions.IgnoreCase))
                {
                    tags.Add("Easy care");
                }

                if (Regex.IsMatch(materialContent, @"\bwełna\b", RegexOptions.IgnoreCase))
                {
                    tags.Add("Wełna");
                }
            }

            var imagesSelector =
                productDoc.QuerySelectorAll(
                    ".row.desktop-part-gallery > div");

            var imageUrls = imagesSelector.Select(htmlDivWithImage =>
            {
                var imageSelector = htmlDivWithImage.QuerySelector("a > picture > img");
                var imageUrl = imageSelector?.GetAttribute("src");
                return imageUrl;
            }).Where(url => url is not null).Cast<string>();

            var description = productDoc.QuerySelector("#collapse_description > div > p:nth-child(1)")?.TextContent?
                .Trim();

            var product = new ProductSnapshot
            {
                Id = $"BYT-{id}",
                Name = name,
                Description = description,
                Brand = "Bytom",
                Url = href,
                CurrentPrice = currentPrice,
                OriginalPrice = originalPrice ?? currentPrice,
                Omnibus30DaysPrice = omnibus30DaysPrice ?? currentPrice,
                ImageUrls = imageUrls.ToArray(),
                Sizes = sizes.ToArray(),
                Tags = tags.ToArray(),
                Timestamp = timestamp,
                IsOutlet = isOutlet
            };

            products.Add(product);
        }

        return products;
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