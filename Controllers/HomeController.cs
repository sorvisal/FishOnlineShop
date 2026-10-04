using System.Diagnostics;
using FishOnlineShop.Data;
using FishOnlineShop.Models;
using FishOnlineShop.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FishOnlineShop.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ApplicationDbContext context, ILogger<HomeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var inStock = _context.FishProducts
                .AsNoTracking()
                .Include(p => p.Category)
                .Where(p => p.Stock > 0);

            // Best sellers first (units sold from OrderDetails); newest fish fill in until there are sales.
            var top = await inStock
                .OrderByDescending(p => p.OrderDetails.Sum(od => (int?)od.Quantity) ?? 0)
                .ThenByDescending(p => p.CreatedAt)
                .Take(8)
                .ToListAsync(cancellationToken);

            var featured = await inStock
                .OrderByDescending(p => p.Price)
                .Take(5)
                .ToListAsync(cancellationToken);

            var categories = await _context.Categories
                .AsNoTracking()
                .OrderBy(c => c.CategoryName)
                .Select(c => new CategoryTileViewModel
                {
                    CategoryId = c.CategoryId,
                    CategoryName = c.CategoryName,
                    Description = c.Description,
                    ProductCount = c.FishProducts.Count()
                })
                .ToListAsync(cancellationToken);

            for (var i = 0; i < categories.Count; i++)
            {
                categories[i].UseAltAccent = i % 2 == 1;
            }

            var vm = new HomeIndexViewModel
            {
                CategoryTiles = categories,
                TopProducts = top,
                FeaturedProduct = featured.FirstOrDefault(),
                FeaturedSide = featured.Skip(1).ToList(),
                CategoryCount = categories.Count,
                TotalProductCount = await _context.FishProducts
                    .AsNoTracking()
                    .CountAsync(cancellationToken)
            };

            return View(vm);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult About()
        {
            ViewData["Title"] = "About us";
            return View();
        }

        /// <summary>
        /// Static contact details. There is deliberately no form here: the only form on the
        /// site that pretends to send something (the footer newsletter) already tells the
        /// visitor it is not wired up, and a second one would just be misleading.
        /// </summary>
        public IActionResult Contact()
        {
            ViewData["Title"] = "Contact";
            return View();
        }

        /// <summary>
        /// Placeholder for the footer newsletter box. Nothing is persisted yet, so the
        /// visitor is told plainly instead of being told they were subscribed.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Newsletter(string? email)
        {
            TempData["ErrorMessage"] = string.IsNullOrWhiteSpace(email)
                ? "Please enter an email address."
                : "Newsletter sign-up is not wired up in this build yet. Thanks for trying.";

            return RedirectToAction(nameof(Index));
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}