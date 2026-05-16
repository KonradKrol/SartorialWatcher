using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using SartorialWatcher.Core.Core;
using SartorialWatcher.Core.Utils;

namespace SartorialWatcher.Core.Scrapers;

// TODO: Add logs
public class WolczankaScraper(IHttpClientFactory httpFactory, ILogger<WolczankaScraper> logger) : IScraper
{
    private HttpClient _http = httpFactory.CreateClient("scraper");
    private readonly HtmlParser _parser = new();
    private readonly SemaphoreSlim _semaphore = new(8);

    public async Task<ScraperResult> ScrapeAsync(ScrapingContext context)
    {
        var baseUrl = context.Url;
        var isOutlet = baseUrl.ToString().Contains("outlet");

        using (logger.BeginScope(new Dictionary<string, object>
               {
                   ["BaseUrl"] = baseUrl
               }))
        {
            List<ProductSnapshot> firstPageProducts;
            int maxPage = 1;
            using (logger.BeginScope(new Dictionary<string, object>
                       {
                           ["Page"] = maxPage
                       }
                   ))
            {
                var firstPageScraperResult = await ScrapePage(baseUrl, isOutlet, maxPage);
                if (firstPageScraperResult is null)
                {
                    logger.LogWarning("First PageScrapingResult is null");
                    throw new Exception("Failed to scrape");
                }

                firstPageProducts = firstPageScraperResult.Products;
                maxPage = firstPageScraperResult.MaxDisplayedPage ?? 1; // TODO: Czy to dobre, by było hardcoded 5?
            }

            var startingPage = 2;

            var products = new List<ProductSnapshot>();

            if (maxPage > 1)
            {
                while (true)
                {
                    var asyncTasks = Enumerable.Range(startingPage, maxPage + 1 - startingPage)
                        .Select(async page =>
                        {
                            using (logger.BeginScope(new Dictionary<string, object>
                                       {
                                           ["Page"] = page
                                       }
                                   ))
                            {
                                logger.LogInformation("Scraping a page");

                                var pageScrapingResult = await ScrapePage(baseUrl, isOutlet, page);

                                if (pageScrapingResult is null)
                                {
                                    logger.LogWarning("PageScrapingResult is null");
                                    return null;
                                }

                                logger.LogInformation(
                                    "Scrapped {ProductsCount} products. Max displayed page is {MaxDisplayedPage}",
                                    pageScrapingResult.Products.Count, pageScrapingResult.MaxDisplayedPage);

                                return pageScrapingResult.Products.Count == 0 ? null : pageScrapingResult;
                            }
                        });

                    var results = await Task.WhenAll(asyncTasks);
                    if (results.Length == 0)
                    {
                        break;
                    }

                    var effectiveResults = results.Where(product => product is not null).Cast<PageScraperResult>()
                        .ToList();
                    if (effectiveResults.Count == 0)
                    {
                        break;
                    }

                    var maxFoundPage = effectiveResults.Select(result => result.MaxDisplayedPage).Max();
                    var effectivePagesCount = effectiveResults.Count;
                    var scrapedProducts =
                        effectiveResults.SelectMany(scraperResult => scraperResult.Products).ToList();

                    if (startingPage == 2)
                    {
                        scrapedProducts = firstPageProducts.Concat(scrapedProducts).ToList();
                    }

                    products.AddRange(scrapedProducts);

                    logger.LogInformation("Partial scraping: scraped {ProductsCount} products at {PagesCount} pages",
                        scrapedProducts.Count,
                        effectivePagesCount);

                    if (maxFoundPage is null || maxFoundPage == maxPage)
                    {
                        break;
                    }

                    startingPage = maxPage + 1;
                    maxPage = (int)maxFoundPage;
                }
            }
            else
            {
                products.AddRange(firstPageProducts);
            }

            var pagesCount = maxPage;

            logger.LogInformation("Scraped {ProductsCount} products on {PagesCount} pages", products.Count,
                pagesCount);

            return new ScraperResult { Products = products, PagesCount = pagesCount };
        }
    }

    private async Task<PageScraperResult?> ScrapePage(Uri url, bool isOutlet, int page)
    {
        var finalUrl = QueryHelpers.AddQueryString(url.ToString(), "page", page.ToString());
        var httpResponse = await _http.GetAsync(finalUrl); // TODO: cancellation token, headers
        httpResponse.EnsureSuccessStatusCode();
        if (httpResponse.StatusCode == HttpStatusCode.NotFound) return null;
        var html = await httpResponse.Content.ReadAsStringAsync();

        var doc = await _parser.ParseDocumentAsync(html);

        var pageNumbersSelector = doc.QuerySelectorAll(".products-control-container > div > nav > ul > li");

        int? maxPageNumber = null;
        try
        {
            maxPageNumber = pageNumbersSelector.Where(pageNumberItem =>
            {
                var textContent = pageNumberItem.TextContent;
                return Regex.IsMatch(textContent, @"^\d+$");
            }).Select(pageNumber => int.Parse(pageNumber.TextContent)).Max();
        }
        catch (InvalidOperationException)
        {
            logger.LogInformation("Missing max page number");
        }

        var timestamp = DateTime.UtcNow;

        var cards = doc.QuerySelectorAll(
            ".grid-row.product-page-items .product-page-item");

        var asyncTasks = cards.Select(async card =>
        {
            try
            {
                return await ScrapeProduct(card, isOutlet, timestamp);
            }
            catch (Exception
                   ex)
            {
                logger.LogError(ex, "Failed scraping a card product");
                return null;
            }
        });

        var nullableProducts = (await Task.WhenAll(asyncTasks)).ToList();
        var products = nullableProducts.Where(product => product is not null).Cast<ProductSnapshot>().ToList();

        var nullProductsCount = nullableProducts.Count - products.Count;
        if (nullProductsCount > 0)
        {
            logger.LogWarning("{NullProductsCount} products failed to scrape", nullProductsCount);
        }

        return new PageScraperResult(Products: products, MaxDisplayedPage: maxPageNumber);
    }

    private async Task<ProductSnapshot?> ScrapeProduct(IElement productHtmlCard, bool isOutlet, DateTime timestamp)
    {
        await _semaphore.WaitAsync();

        var id = productHtmlCard.GetAttribute("data-id") ?? throw new Exception("Id is null");
        var name = productHtmlCard.GetAttribute("data-item-name")?.ToSentenceCaseInvariant() ??
                   throw new Exception("Name is null");
        var currentPriceString =
            productHtmlCard.GetAttribute("data-price") ?? throw new Exception("Current price is null");
        var currentPrice = decimal.Parse(currentPriceString);

        var originalPriceString =
            productHtmlCard.QuerySelector(".product-price-omnibus > div:nth-child(2)")?.TextContent?.Trim();
        var omnibus30DaysPriceString = productHtmlCard.QuerySelector(
            ".product-price-omnibus > div:nth-child(1)")?.TextContent.Trim();

        var originalPrice = ParsePolishPrice(originalPriceString);
        var omnibus30DaysPrice = ParsePolishPrice(omnibus30DaysPriceString);

        var href = productHtmlCard.QuerySelector("a")?.GetAttribute("href") ?? throw new Exception("Href is null");

        string productSiteHtml;

        try
        {
            var response = await _http.GetAsync(href);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            if ((int)response.StatusCode >= 500)
            {
                logger.LogWarning(
                    "Temporary server error {StatusCode} for {Href}",
                    response.StatusCode,
                    href);

                return null;
            }

            response.EnsureSuccessStatusCode();
            productSiteHtml = await _http.GetStringAsync(href);
        }
        finally
        {
            _semaphore.Release();
        }

        var productDoc = await _parser.ParseDocumentAsync(productSiteHtml);

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
            Id = $"WOL-{id}",
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
        return product;
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

internal record PageScraperResult(List<ProductSnapshot> Products, int? MaxDisplayedPage);