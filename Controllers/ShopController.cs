using FishOnlineShop.Data;
using FishOnlineShop.Models;
using FishOnlineShop.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FishOnlineShop.Controllers
{
    public class ShopController : Controller
    {
        public const int PageSize = 12;

        private const int MaxPrice = 99999999;
        private const int RelatedProductCount = 4;

        private readonly ApplicationDbContext _context;

        public ShopController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Shop
        public async Task<IActionResult> Index(
            string? search,
            int[]? categoryIds,
            string? categories,
            int? categoryId,
            decimal? minPrice,
            decimal? maxPrice,
            bool inStockOnly = false,
            string? sort = null,
            int page = 1,
            CancellationToken cancellationToken = default)
        {
            var selectedIds = NormaliseCategories(categoryIds, categories, categoryId);
            var (from, to) = NormalisePriceRange(minPrice, maxPrice);
            var sortKey = ShopSort.Normalize(sort);

            var query = _context.FishProducts
                .AsNoTracking()
                .Include(p => p.Category)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(p => p.FishName.Contains(term)
                                      || (p.Description != null && p.Description.Contains(term)));
            }

            if (selectedIds.Length > 0)
            {
                query = query.Where(p => selectedIds.Contains(p.CategoryId));
            }

            if (from.HasValue)
            {
                query = query.Where(p => p.Price >= from.Value);
            }

            if (to.HasValue)
            {
                query = query.Where(p => p.Price <= to.Value);
            }

            if (inStockOnly)
            {
                query = query.Where(p => p.Stock > 0);
            }

            query = sortKey switch
            {
                ShopSort.PriceAscending => query.OrderBy(p => p.Price).ThenBy(p => p.ProductId),
                ShopSort.PriceDescending => query.OrderByDescending(p => p.Price).ThenBy(p => p.ProductId),
                _ => query.OrderByDescending(p => p.CreatedAt).ThenBy(p => p.ProductId)
            };

            var totalItems = await query.CountAsync(cancellationToken);
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)PageSize));

            // Guard against a hand edited ?page=9999.
            var currentPage = page < 1 ? 1 : Math.Min(page, totalPages);

            var products = await query
                .Skip((currentPage - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync(cancellationToken);

            // Facet counts are taken over the whole catalogue, then the selection is applied in memory
            // because EF cannot translate an in-memory Contains over the projection.
            var categoryFacets = (await _context.Categories
                    .AsNoTracking()
                    .OrderBy(c => c.CategoryName)
                    .Select(c => new CategoryFilterItem
                    {
                        CategoryId = c.CategoryId,
                        CategoryName = c.CategoryName,
                        ProductCount = c.FishProducts.Count()
                    })
                    .ToListAsync(cancellationToken))
                .Select(c =>
                {
                    c.IsSelected = Array.IndexOf(selectedIds, c.CategoryId) >= 0;
                    return c;
                })
                .ToList();

            var vm = new ShopIndexViewModel
            {
                Products = products,
                Categories = categoryFacets,
                SortOptions = BuildSortOptions(),
                SelectedCategoryIds = selectedIds,
                Search = string.IsNullOrWhiteSpace(search) ? null : search.Trim(),
                MinPrice = from,
                MaxPrice = to,
                InStockOnly = inStockOnly,
                Sort = sortKey,
                Pagination = new PaginationViewModel
                {
                    Page = currentPage,
                    TotalPages = totalPages,
                    TotalItems = totalItems,
                    PageSize = PageSize,
                    RouteValues = BuildRouteValues(
                        search, selectedIds, from, to, inStockOnly, sortKey)
                }
            };

            return View(vm);
        }

        // GET: Shop/Details/5
        public async Task<IActionResult> Details(int? id, CancellationToken cancellationToken = default)
        {
            if (!id.HasValue || id.Value <= 0)
            {
                return NotFound();
            }

            var product = await _context.FishProducts
                .AsNoTracking()
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.ProductId == id.Value, cancellationToken);

            if (product is null)
            {
                return NotFound();
            }

            var related = await _context.FishProducts
                .AsNoTracking()
                .Include(p => p.Category)
                .Where(p => p.CategoryId == product.CategoryId
                            && p.ProductId != product.ProductId
                            && p.Stock > 0)
                .OrderByDescending(p => p.CreatedAt)
                .Take(RelatedProductCount)
                .ToListAsync(cancellationToken);

            var vm = new ShopDetailsViewModel
            {
                Product = product,
                RelatedProducts = related,
                Breadcrumb = new BreadcrumbViewModel(
                    new BreadcrumbItem("Home", "Home", "Index"),
                    new BreadcrumbItem("Shop", "Shop", "Index",
                        new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                        {
                            ["categoryId"] = product.CategoryId.ToString()
                        }),
                    new BreadcrumbItem(product.FishName))
            };

            return View(vm);
        }

        /// <summary>
        /// Merges the three ways a category selection can arrive:
        /// <list type="bullet">
        /// <item><c>categoryIds</c> - repeated query keys, produced by the sidebar checkboxes.</item>
        /// <item><c>categories</c> - comma separated, produced by the pagination links. ASP.NET
        /// Core cannot bind a single comma separated value to <c>int[]</c>, so the links use
        /// their own parameter.</item>
        /// <item><c>categoryId</c> - single id, used by the category tiles and footer links.</item>
        /// </list>
        /// </summary>
        private static int[] NormaliseCategories(int[]? categoryIds, string? categories, int? categoryId)
        {
            var selected = new HashSet<int>();

            if (categoryIds is not null)
            {
                foreach (var id in categoryIds)
                {
                    if (id > 0)
                    {
                        selected.Add(id);
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(categories))
            {
                foreach (var part in categories.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    if (int.TryParse(part, out var id) && id > 0)
                    {
                        selected.Add(id);
                    }
                }
            }

            if (categoryId is > 0)
            {
                selected.Add(categoryId.Value);
            }

            return selected.OrderBy(id => id).ToArray();
        }

        private static (decimal? From, decimal? To) NormalisePriceRange(decimal? minPrice, decimal? maxPrice)
        {
            var from = minPrice is > 0 ? minPrice : null;
            var to = maxPrice is > 0 ? maxPrice : null;

            if (from > MaxPrice)
            {
                from = MaxPrice;
            }

            if (to > MaxPrice)
            {
                to = MaxPrice;
            }

            // A reversed range would silently return nothing, so swap it instead.
            if (from.HasValue && to.HasValue && from > to)
            {
                (from, to) = (to, from);
            }

            return (from, to);
        }

        /// <summary>
        /// Options for the sort dropdown. The selected value comes from the model through
        /// asp-for, so no state is baked into the list here.
        /// </summary>
        private static List<SelectListItem> BuildSortOptions()
            => new()
            {
                new SelectListItem { Value = ShopSort.Newest, Text = "Newest first" },
                new SelectListItem { Value = ShopSort.PriceAscending, Text = "Price: low to high" },
                new SelectListItem { Value = ShopSort.PriceDescending, Text = "Price: high to low" }
            };

        /// <summary>
        /// Query string that reproduces the current filter. Every pagination link is built from
        /// this so changing page never drops the filters.
        /// </summary>
        private static Dictionary<string, string?> BuildRouteValues(
            string? search,
            int[] selectedIds,
            decimal? minPrice,
            decimal? maxPrice,
            bool inStockOnly,
            string sort)
        {
            var route = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

            if (!string.IsNullOrWhiteSpace(search))
            {
                route["search"] = search.Trim();
            }

            if (selectedIds.Length > 0)
            {
                // Comma separated under its own key, because the pagination links carry one
                // value per key and a single comma separated value will not bind to int[].
                route["categories"] = string.Join(",", selectedIds);
            }

            if (minPrice.HasValue)
            {
                route["minPrice"] = minPrice.Value.ToString("0.##");
            }

            if (maxPrice.HasValue)
            {
                route["maxPrice"] = maxPrice.Value.ToString("0.##");
            }

            if (inStockOnly)
            {
                route["inStockOnly"] = "true";
            }

            // Matches the field name the sort select posts (asp-for="Sort"), so links and form
            // submissions produce the same query string casing.
            route["Sort"] = sort;

            return route;
        }
    }
}