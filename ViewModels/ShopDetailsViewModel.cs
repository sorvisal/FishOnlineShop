using FishOnlineShop.Models;

namespace FishOnlineShop.ViewModels
{
    /// <summary>Input for <c>Shop/Details.cshtml</c>.</summary>
    public class ShopDetailsViewModel
    {
        public FishProduct Product { get; set; } = null!;

        /// <summary>Other in-stock products from the same category.</summary>
        public IReadOnlyList<FishProduct> RelatedProducts { get; set; } = Array.Empty<FishProduct>();

        public BreadcrumbViewModel Breadcrumb { get; set; } = new();

        public bool IsOutOfStock => Product.Stock <= 0;

        /// <summary>Upper bound for the quantity selector: never more than we actually hold.</summary>
        public int MaxQuantity => Math.Clamp(Product.Stock, 1, 99);

        public string StockLabel => Product.Stock switch
        {
            <= 0 => "Out of stock",
            1 => "Only 1 left",
            <= 5 => $"Only {Product.Stock} left",
            _ => $"In stock: {Product.Stock}"
        };

        public string StockBadgeClass => Product.Stock switch
        {
            <= 0 => "aq-badge-out",
            <= 5 => "aq-badge-warn",
            _ => "aq-badge-in"
        };
    }
}