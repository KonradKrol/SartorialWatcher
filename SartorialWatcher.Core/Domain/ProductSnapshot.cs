namespace SartorialWatcher.Core.Domain;

public class ProductSnapshot : IEquatable<ProductSnapshot>
{
    public required string Id { get; set; }
    public required string Brand { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required string Url { get; set; }
    public string? ImageUrl { get; set; }
    public string[] Sizes { get; init; } = [];
    public string[] Tags { get; init; } = [];
    public required decimal CurrentPrice { get; set; }
    public required decimal OriginalPrice { get; set; }
    public required decimal Omnibus30DaysPrice { get; set; }
    public required DateTimeOffset Timestamp { get; set; }
    public Sleeves? Sleeves { get; set; }
    public required bool IsOutlet { get; set; }

    public decimal Discount => 1 - decimal.Round(CurrentPrice / OriginalPrice * 100) / 100;
    public decimal Omnibus30DaysDiscount => 1 - decimal.Round(CurrentPrice / Omnibus30DaysPrice * 100) / 100;

    public bool Equals(ProductSnapshot? other)
    {
        return Id == other?.Id;
    }

    public override bool Equals(object? obj)
    {
        if (obj is ProductSnapshot product)
        {
            return Id == product.Id;
        }

        return false;
    }
    
    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }
}

public enum Sleeves
{
    Long,
    Short,
}