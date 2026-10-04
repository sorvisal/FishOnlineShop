using FishOnlineShop.Models;

namespace FishOnlineShop.ViewModels
{
    public class HomeIndexViewModel
    {
        public List<CategoryTileViewModel> CategoryTiles { get; set; } = new();

        public List<FishProduct> TopProducts { get; set; } = new();

        public FishProduct? FeaturedProduct { get; set; }

        public List<FishProduct> FeaturedSide { get; set; } = new();

        public int TotalProductCount { get; set; }

        public int CategoryCount { get; set; }
    }
}