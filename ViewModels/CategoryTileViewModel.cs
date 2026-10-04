namespace FishOnlineShop.ViewModels
{
    /// <summary>Input for <c>Shared/_CategoryTile.cshtml</c>.</summary>
    public class CategoryTileViewModel
    {
        public int CategoryId { get; set; }

        public string CategoryName { get; set; } = string.Empty;

        public string? Description { get; set; }

        public int ProductCount { get; set; }

        /// <summary>Alternates the tile accent so a row of tiles is not monotonous.</summary>
        public bool UseAltAccent { get; set; }

        /// <summary>Controller that renders the category listing (storefront by default).</summary>
        public string ControllerName { get; set; } = "Shop";

        public string ActionName { get; set; } = "Index";
    }
}