using Microsoft.AspNetCore.Mvc.Rendering;

namespace FishOnlineShop.ViewModels
{
    public class CategoryFilterItem
    {
        public int CategoryId { get; set; }

        public string CategoryName { get; set; } = string.Empty;

        public int ProductCount { get; set; }

        public bool IsSelected { get; set; }
    }

    /// <summary>Input for <c>Shop/Index.cshtml</c>: the filtered page plus the filter state.</summary>
    public class ShopIndexViewModel
    {
        public IReadOnlyList<Models.FishProduct> Products { get; set; } = Array.Empty<Models.FishProduct>();

        public IReadOnlyList<CategoryFilterItem> Categories { get; set; } = Array.Empty<CategoryFilterItem>();

        /// <summary>Options for the sort dropdown. A plain list is used because that is
    /// what the select tag helper consumes reliably; the current value is applied
    /// through asp-for.</summary>
    public List<SelectListItem> SortOptions { get; set; } = new();

        public int[] SelectedCategoryIds { get; set; } = Array.Empty<int>();

        public string? Search { get; set; }

        public decimal? MinPrice { get; set; }

        public decimal? MaxPrice { get; set; }

        public bool InStockOnly { get; set; }

        public string Sort { get; set; } = ShopSort.Newest;

        public PaginationViewModel Pagination { get; set; } = new();

        /// <summary>Drives which empty state is shown and whether "Clear filters" is offered.</summary>
        public bool HasActiveFilters =>
            !string.IsNullOrWhiteSpace(Search)
            || SelectedCategoryIds.Length > 0
            || MinPrice.HasValue
            || MaxPrice.HasValue
            || InStockOnly;

        /// <summary>Human readable summary of the active filters, e.g. "2 categories, in stock only".</summary>
        public string ActiveFilterSummary
        {
            get
            {
                var parts = new List<string>();

                if (SelectedCategoryIds.Length == 1)
                {
                    var name = Categories.FirstOrDefault(c => c.CategoryId == SelectedCategoryIds[0])?.CategoryName;
                    parts.Add(name ?? "1 category");
                }
                else if (SelectedCategoryIds.Length > 1)
                {
                    parts.Add($"{SelectedCategoryIds.Length} categories");
                }

                if (MinPrice.HasValue || MaxPrice.HasValue)
                {
                    var from = MinPrice.HasValue ? MinPrice.Value.ToString("0.##") : "0";
                    var to = MaxPrice.HasValue ? MaxPrice.Value.ToString("0.##") : "any";
                    parts.Add($"${from} - ${to}");
                }

                if (InStockOnly)
                {
                    parts.Add("in stock only");
                }

                if (!string.IsNullOrWhiteSpace(Search))
                {
                    parts.Add($"matching \"{Search.Trim()}\"");
                }

                return string.Join(", ", parts);
            }
        }
    }

    /// <summary>The sort keys accepted by <c>ShopController.Index</c>.</summary>
    public static class ShopSort
    {
        public const string Newest = "newest";

        public const string PriceAscending = "price-asc";

        public const string PriceDescending = "price-desc";

        public static bool IsValid(string? sort)
            => sort is Newest or PriceAscending or PriceDescending;

        public static string Normalize(string? sort)
            => IsValid(sort) ? sort! : Newest;
    }
}