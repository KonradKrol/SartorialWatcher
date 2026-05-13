using SartorialWatcher.Core.Core;

namespace SartorialWatcher.Core.Scrapers;

public class MockedScraper : IScraper
{
    public async Task<ScraperResult> ScrapeAsync(ScrapingContext context)
    {
        return new ScraperResult
        {
            Products = new List<ProductSnapshot>()
            {
                new()
                {
                    Id = "12345", // Często ze strony, np. z divów
                    Url = "https://www.wolczanka.pl/koszula-lniana-188-194-40-szt-1299",
                    Name = "Koszula Lniana",
                    Brand = "Wólczanka",
                    Description = "Stworzone z wysokiej jakości tkaniny.",
                    ImageUrl = null,
                    CurrentPrice = 12.99m,
                    Sizes =
                    [
                        "188-194/40"
                    ],
                    Tags =
                    [
                        "Len"
                    ],
                    OriginalPrice = 0,
                    Omnibus30DaysPrice = 0,
                    Timestamp = default,
                    IsOutlet = true,
                }
            }
        };
    }
}