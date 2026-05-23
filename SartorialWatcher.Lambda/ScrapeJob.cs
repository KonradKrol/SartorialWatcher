using System.ComponentModel.DataAnnotations;

namespace SartorialWatcher.Lambda;

internal record ScrapeJob(string ShopName, [Url] string Url);