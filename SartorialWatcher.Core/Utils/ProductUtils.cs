using SartorialWatcher.Core.Domain;

namespace SartorialWatcher.Core.Utils;

public static class ProductUtils
{
    extension(IEnumerable<ProductSnapshot> products)
    {
        public IOrderedEnumerable<ProductSnapshot> OrderByPrice(bool descending = false)
        {
            return descending
                ? products
                    .OrderByDescending(product => product.CurrentPrice)
                    .ThenBy(product => product.Id)
                : products
                    .OrderBy(product => product.CurrentPrice)
                    .ThenBy(product => product.Id);
        }
    }
}