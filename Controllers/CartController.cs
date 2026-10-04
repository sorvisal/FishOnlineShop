using FishOnlineShop.Data;
using FishOnlineShop.Models;
using FishOnlineShop.Services;
using FishOnlineShop.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FishOnlineShop.Controllers
{
    /// <summary>
    /// Cart actions. Every write is a POST followed by a redirect, so a refresh never
    /// re-submits a mutation, and no action trusts a price or a name coming from the client.
    /// </summary>
    public class CartController : Controller
    {
        private const int MaxQuantityPerLine = 99;

        private readonly ApplicationDbContext _context;
        private readonly ICartSession _cart;
        private readonly ILogger<CartController> _logger;

        public CartController(
            ApplicationDbContext context,
            ICartSession cart,
            ILogger<CartController> logger)
        {
            _context = context;
            _cart = cart;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var model = await BuildCartAsync();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(int id, int quantity)
        {
            if (id <= 0)
            {
                return CartError("That product could not be added to your cart.");
            }

            var product = await _context.FishProducts
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product is null)
            {
                return CartError("Sorry, that product is no longer available.");
            }

            if (product.Stock <= 0)
            {
                return CartError($"“{product.FishName}” is out of stock and could not be added to your cart.", id);
            }

            var requested = quantity <= 0 ? 1 : quantity;
            var clamped = Math.Min(requested, Math.Min(product.Stock, MaxQuantityPerLine));

            var existing = _cart.GetLines().FirstOrDefault(line => line.ProductId == id);
            var combined = (existing?.Quantity ?? 0) + clamped;

            if (existing is not null && combined > product.Stock)
            {
                // Already in the cart and the combined amount is more than we have, so pin
                // the line to the stock instead of rejecting the add.
                _cart.SetQuantity(id, product.Stock);
                TempData["WarningMessage"] =
                    $"Only {product.Stock} of “{product.FishName}” in stock, so the cart now holds {product.Stock}.";
            }
            else
            {
                // Add is an increment: the session merges it into an existing line, so
                // passing the combined total here would count the previous quantity twice.
                _cart.Add(id, clamped);
                TempData["SuccessMessage"] = $"Added “{product.FishName}” to your cart.";
            }

            return RedirectToAction("Details", "Shop", new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(int id, int quantity)
        {
            if (id <= 0)
            {
                return RedirectToAction(nameof(Index));
            }

            var product = await _context.FishProducts
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product is null)
            {
                // The line is gone; say so rather than silently dropping it.
                _cart.Remove(id);
                TempData["WarningMessage"] = "That product is no longer available and was removed from your cart.";
                return RedirectToAction(nameof(Index));
            }

            if (product.Stock <= 0)
            {
                _cart.Remove(id);
                TempData["WarningMessage"] = $"“{product.FishName}” is out of stock and was removed from your cart.";
                return RedirectToAction(nameof(Index));
            }

            if (quantity <= 0)
            {
                _cart.Remove(id);
                TempData["SuccessMessage"] = $"Removed “{product.FishName}” from your cart.";
                return RedirectToAction(nameof(Index));
            }

            var clamped = Math.Min(quantity, Math.Min(product.Stock, MaxQuantityPerLine));
            _cart.SetQuantity(id, clamped);

            if (clamped < quantity)
            {
                TempData["WarningMessage"] =
                    $"Only {product.Stock} of “{product.FishName}” in stock, so the quantity was set to {product.Stock}.";
            }
            else
            {
                TempData["SuccessMessage"] = $"Updated “{product.FishName}” quantity to {clamped}.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int id)
        {
            if (id <= 0)
            {
                return RedirectToAction(nameof(Index));
            }

            var name = await _context.FishProducts
                .AsNoTracking()
                .Where(p => p.ProductId == id)
                .Select(p => p.FishName)
                .FirstOrDefaultAsync();

            _cart.Remove(id);
            TempData["SuccessMessage"] = name is null
                ? "Item removed from your cart."
                : $"Removed “{name}” from your cart.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Clear()
        {
            _cart.Clear();
            TempData["SuccessMessage"] = "Your cart is now empty.";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Loads the cart, joins it to live product data and reconciles anything that
        /// changed in the catalogue: deleted or out of stock lines are dropped, and lines
        /// above the current stock are trimmed. The reconciled state is written back to
        /// the session so the badge and every later request agree with what is shown.
        /// </summary>
        private async Task<CartIndexViewModel> BuildCartAsync()
        {
            var model = new CartIndexViewModel();

            var cartLines = _cart.GetLines();
            if (cartLines.Count == 0)
            {
                return model;
            }

            var ids = cartLines.Select(line => line.ProductId).ToArray();
            var products = await _context.FishProducts
                .AsNoTracking()
                .Where(p => ids.Contains(p.ProductId))
                .ToDictionaryAsync(p => p.ProductId);

            var lines = new List<CartItemViewModel>();
            var reconciled = new List<CartLine>();
            var dropped = new List<string>();
            var trimmed = new List<string>();

            foreach (var line in cartLines)
            {
                if (!products.TryGetValue(line.ProductId, out var product))
                {
                    dropped.Add($"item #{line.ProductId}");
                    continue;
                }

                if (product.Stock <= 0)
                {
                    dropped.Add($"“{product.FishName}” (out of stock)");
                    continue;
                }

                var quantity = Math.Min(line.Quantity, product.Stock);
                if (quantity < line.Quantity)
                {
                    trimmed.Add(product.FishName);
                }

                lines.Add(new CartItemViewModel
                {
                    Product = product,
                    Quantity = quantity,
                    QuantityWasReduced = quantity < line.Quantity
                });

                reconciled.Add(new CartLine { ProductId = product.ProductId, Quantity = quantity });
            }

            if (dropped.Count > 0)
            {
                _logger.LogInformation("Dropped {Count} unavailable cart line(s).", dropped.Count);
                model.Adjustments.Add(
                    $"{DescribeList(dropped)} {(dropped.Count == 1 ? "was" : "were")} no longer available and {(dropped.Count == 1 ? "has" : "have")} been removed from your cart.");
            }

            if (trimmed.Count > 0)
            {
                model.Adjustments.Add(
                    $"{DescribeList(trimmed)}: the quantity {(trimmed.Count == 1 ? "was" : "were")} reduced to match the stock currently available.");
            }

            if (model.Adjustments.Count > 0)
            {
                _cart.Clear();
                foreach (var line in reconciled)
                {
                    _cart.Add(line.ProductId, line.Quantity);
                }

                TempData["WarningMessage"] = string.Join(" ", model.Adjustments);
            }

            model.Lines = lines;
            model.TotalItems = lines.Sum(line => line.Quantity);
            model.SubTotal = lines.Sum(line => line.LineTotal);
            model.Shipping = 0m;
            model.Total = model.SubTotal + model.Shipping;

            return model;
        }

        private static string DescribeList(IReadOnlyCollection<string> names)
        {
            // Name at most three entries, then summarise the rest.
            var named = names.Take(3).ToList();
            var remainder = names.Count - named.Count;

            return remainder > 0
                ? $"{string.Join(", ", named)} and {remainder} more"
                : string.Join(", ", named);
        }

        private IActionResult CartError(string message, int? returnToProductId = null)
        {
            TempData["ErrorMessage"] = message;
            return returnToProductId.HasValue
                ? RedirectToAction("Details", "Shop", new { id = returnToProductId.Value })
                : RedirectToAction(nameof(Index));
        }
    }
}