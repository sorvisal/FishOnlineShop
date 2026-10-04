using System.Security.Claims;
using FishOnlineShop.Data;
using FishOnlineShop.Models;
using FishOnlineShop.Services;
using FishOnlineShop.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FishOnlineShop.Controllers
{
    [Authorize]
    public class CheckoutController : Controller
    {
        private const string PendingStatus = "Pending";

        private readonly ApplicationDbContext _context;
        private readonly ICartSession _cart;
        private readonly ILogger<CheckoutController> _logger;

        public CheckoutController(
            ApplicationDbContext context,
            ICartSession cart,
            ILogger<CheckoutController> logger)
        {
            _context = context;
            _cart = cart;
            _logger = logger;
        }

        // -------------------------------------------------------------------- Index

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var cart = await BuildCartAsync();

            if (cart.Summary.IsEmpty)
            {
                TempData["ErrorMessage"] = "Your cart is empty. Add a few fish before checking out.";
                return RedirectToAction("Index", "Cart");
            }

            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserId == CurrentUserId);

            ViewBag.PaymentMethods = PaymentMethods.ToSelectList();
            WarnAboutAdjustments(cart);

            var model = new CheckoutViewModel
            {
                // Pre-fill from the account, but only where the customer actually saved it.
                ReceiverName = user?.FullName ?? string.Empty,
                Phone = user?.Phone ?? string.Empty,
                ShippingAddress = user?.Address ?? string.Empty,
                PaymentMethod = PaymentMethods.CashOnDelivery,
                Cart = cart.Summary
            };

            return View(model);
        }

        // -------------------------------------------------------------- Place order

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(CheckoutViewModel model)
        {
            var cart = await BuildCartAsync();

            if (cart.Summary.IsEmpty)
            {
                TempData["ErrorMessage"] = "Your cart is empty. Add a few fish before checking out.";
                return RedirectToAction("Index", "Cart");
            }

            ViewBag.PaymentMethods = PaymentMethods.ToSelectList();

            // The money always comes from the rebuilt cart, never from the posted form.
            model.Cart = cart.Summary;
            WarnAboutAdjustments(cart);

            if (!ModelState.IsValid)
            {
                return View("Index", model);
            }

            if (!PaymentMethods.IsAllowed(model.PaymentMethod))
            {
                ModelState.AddModelError(nameof(model.PaymentMethod), "Choose one of the available payment methods.");
                return View("Index", model);
            }

            var userId = CurrentUserId;
            if (userId <= 0)
            {
                return Challenge();
            }

            var order = new Order
            {
                // From the auth cookie claims, never from a form field.
                UserId = userId,
                ReceiverName = model.ReceiverName.Trim(),
                Phone = model.Phone.Trim(),
                ShippingAddress = model.ShippingAddress.Trim(),
                PaymentMethod = model.PaymentMethod,
                OrderDate = DateTime.UtcNow,
                OrderStatus = PendingStatus,
                TotalAmount = cart.Summary.Total
            };

            foreach (var line in cart.Summary.Lines)
            {
                order.OrderDetails.Add(new OrderDetail
                {
                    ProductId = line.Product.ProductId,
                    Quantity = line.Quantity,
                    UnitPrice = line.UnitPrice,
                    SubTotal = line.LineTotal
                });
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                foreach (var line in cart.Summary.Lines)
                {
                    // Conditional decrement. If a competing order already took the last
                    // units this matches no rows, so the whole transaction is rolled back
                    // and nobody ends up with oversold stock.
                    var affected = await _context.FishProducts
                        .Where(p => p.ProductId == line.Product.ProductId && p.Stock >= line.Quantity)
                        .ExecuteUpdateAsync(setters => setters
                            .SetProperty(p => p.Stock, p => p.Stock - line.Quantity));

                    if (affected == 0)
                    {
                        await transaction.RollbackAsync();

                        var available = await _context.FishProducts
                            .AsNoTracking()
                            .Where(p => p.ProductId == line.Product.ProductId)
                            .Select(p => (int?)p.Stock)
                            .FirstOrDefaultAsync() ?? 0;

                        TempData["ErrorMessage"] =
                            $"“{line.Product.FishName}” is short: you asked for {line.Quantity} but only {available} left. "
                            + "Your cart has been kept so you can adjust it.";

                        return RedirectToAction(nameof(Index));
                    }
                }

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (DbUpdateException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogWarning(ex, "Checkout rolled back for user {UserId}.", userId);
                TempData["ErrorMessage"] = "Something went wrong while saving your order. Nothing has been charged. Please try again.";
                return RedirectToAction(nameof(Index));
            }

            // Cleared only after the transaction has committed, so a rollback leaves the
            // customer's cart intact.
            _cart.Clear();

            TempData["SuccessMessage"] = $"Order #{order.OrderId} placed. Thank you!";

            return RedirectToAction(nameof(Success), new { id = order.OrderId });
        }

        // ------------------------------------------------------------------ Success

        [HttpGet]
        public async Task<IActionResult> Success(int id)
        {
            var order = await _context.Orders
                .AsNoTracking()
                .Include(o => o.OrderDetails)
                // Without this the Product navigation property stays null, because the
                // context uses AsNoTracking and has lazy-loading proxies switched off.
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order is null || !CanView(order.UserId))
            {
                return NotFound();
            }

            return View(new CheckoutSuccessViewModel
            {
                Order = order,
                // Flattened to plain values so the view cannot dereference a null Product.
                Lines = OrderLineViewModel.FromMany(order.OrderDetails)
            });
        }

        // ------------------------------------------------------------------ Helpers

        private int CurrentUserId
        {
            get
            {
                var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
                return int.TryParse(raw, out var id) ? id : 0;
            }
        }

        private bool IsAdmin => User.IsInRole(UserRoles.Admin);

        /// <summary>
        /// Owner or admin only. Everyone else gets 404 rather than 403 so order ids cannot
        /// be probed to discover whether an order exists.
        /// </summary>
        private bool CanView(int ownerUserId)
            => ownerUserId == CurrentUserId || IsAdmin;

        private void WarnAboutAdjustments(ReconciledCart cart)
        {
            if (cart.Adjustments.Count > 0)
            {
                TempData["WarningMessage"] = string.Join(" ", cart.Adjustments);
            }
        }

        /// <summary>
        /// Rebuilds the cart from live product rows. Prices, names, stock and totals are
        /// always taken from the database; nothing the browser sends about money is read.
        /// Deleted or out of stock lines are dropped, over-asked quantities are trimmed,
        /// and the reconciled cart is written back to the session.
        /// </summary>
        private async Task<ReconciledCart> BuildCartAsync()
        {
            var result = new ReconciledCart();

            var cartLines = _cart.GetLines();
            if (cartLines.Count == 0)
            {
                return result;
            }

            var ids = cartLines.Select(line => line.ProductId).ToArray();
            var products = await _context.FishProducts
                .AsNoTracking()
                .Where(p => ids.Contains(p.ProductId))
                .ToDictionaryAsync(p => p.ProductId);

            var lines = new List<CartItemViewModel>();
            var kept = new List<CartLine>();
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
                    QuantityWasReduced = quantity < line.Quantity,
                    ShowControls = false
                });

                kept.Add(new CartLine { ProductId = product.ProductId, Quantity = quantity });
            }

            if (dropped.Count > 0)
            {
                var was = dropped.Count == 1 ? "was" : "were";
                var has = dropped.Count == 1 ? "has" : "have";
                result.Adjustments.Add(
                    $"{string.Join(", ", dropped.Take(3))} {was} no longer available and {has} been removed from your cart.");
            }

            if (trimmed.Count > 0)
            {
                var was = trimmed.Count == 1 ? "was" : "were";
                result.Adjustments.Add(
                    $"{string.Join(", ", trimmed.Take(3))}: the quantity {was} reduced to match the stock currently available.");
            }

            if (result.Adjustments.Count > 0)
            {
                _cart.Clear();
                foreach (var line in kept)
                {
                    _cart.Add(line.ProductId, line.Quantity);
                }
            }

            result.Summary.Lines = lines;
            result.Summary.TotalItems = lines.Sum(line => line.Quantity);
            result.Summary.SubTotal = lines.Sum(line => line.LineTotal);
            result.Summary.Shipping = 0m;
            result.Summary.Total = result.Summary.SubTotal + result.Summary.Shipping;

            return result;
        }

        /// <summary>A cart summary plus the notes explaining anything that had to change.</summary>
        private sealed class ReconciledCart
        {
            public CartSummaryViewModel Summary { get; } = new();

            public List<string> Adjustments { get; } = new();
        }
    }
}