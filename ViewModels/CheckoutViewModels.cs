using System.ComponentModel.DataAnnotations;
using FishOnlineShop.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FishOnlineShop.ViewModels
{
    /// <summary>
    /// The only two payment methods the server will accept. Anything else posted from the
    /// browser is rejected rather than trusted.
    /// </summary>
    public static class PaymentMethods
    {
        public const string CashOnDelivery = "Cash on Delivery";

        public const string BankTransfer = "Bank Transfer";

        public static readonly string[] All = { CashOnDelivery, BankTransfer };

        public static bool IsAllowed(string? value)
            => !string.IsNullOrWhiteSpace(value)
               && Array.Exists(All, allowed => string.Equals(allowed, value, StringComparison.Ordinal));

        public static IReadOnlyList<SelectListItem> ToSelectList()
            => new List<SelectListItem>
            {
                new() { Text = CashOnDelivery, Value = CashOnDelivery },
                new() { Text = BankTransfer, Value = BankTransfer }
            };
    }

    public class CheckoutViewModel
    {
        [Required(ErrorMessage = "Receiver name is required.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Receiver name must be between 2 and 100 characters.")]
        [Display(Name = "Receiver name")]
        public string ReceiverName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required.")]
        [StringLength(20, MinimumLength = 6, ErrorMessage = "Enter a phone number we can reach you on.")]
        [Display(Name = "Phone")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Shipping address is required.")]
        [StringLength(255, MinimumLength = 5, ErrorMessage = "Shipping address must be between 5 and 255 characters.")]
        [Display(Name = "Shipping address")]
        public string ShippingAddress { get; set; } = string.Empty;

        [Required(ErrorMessage = "Payment method is required.")]
        [Display(Name = "Payment method")]
        public string PaymentMethod { get; set; } = PaymentMethods.CashOnDelivery;

        public CartSummaryViewModel Cart { get; set; } = new();
    }

    /// <summary>
    /// One order line flattened into plain values. The order views deliberately render this
    /// instead of the <see cref="OrderDetail"/> entity so a Razor view never walks a
    /// navigation property such as line.Product.FishName. If the related product was not
    /// loaded, or was removed from the catalogue, the page still renders.
    /// </summary>
    public class OrderLineViewModel
    {
        /// <summary>Falls back to the id when the product row is unavailable.</summary>
        public const string UnknownProductNameFormat = "Product #{0}";

        public int ProductId { get; set; }

        public string ProductName { get; set; } = string.Empty;

        /// <summary>Null when the product has no image, so views can show a placeholder.</summary>
        public string? ImageUrl { get; set; }

        public bool HasImage => !string.IsNullOrWhiteSpace(ImageUrl);

        public int Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public decimal SubTotal { get; set; }

        public static OrderLineViewModel From(OrderDetail detail)
        {
            // detail.Product is null whenever the query did not eagerly load it, which is the
            // normal case for AsNoTracking + no lazy-loading proxies.
            var product = detail.Product;
            var imageUrl = product?.ImageUrl;

            return new OrderLineViewModel
            {
                ProductId = detail.ProductId,
                ProductName = string.IsNullOrWhiteSpace(product?.FishName)
                    ? string.Format(UnknownProductNameFormat, detail.ProductId)
                    : product!.FishName,
                ImageUrl = string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl,
                Quantity = detail.Quantity,
                UnitPrice = detail.UnitPrice,
                SubTotal = detail.SubTotal
            };
        }

        public static List<OrderLineViewModel> FromMany(IEnumerable<OrderDetail> details)
            => details.Select(From).ToList();
    }

    /// <summary>Confirmation page for a freshly placed order.</summary>
    public class CheckoutSuccessViewModel
    {
        public Order Order { get; set; } = null!;

        public List<OrderLineViewModel> Lines { get; set; } = new();

        public bool IsBankTransfer => string.Equals(Order?.PaymentMethod, PaymentMethods.BankTransfer, StringComparison.Ordinal);
    }

    /// <summary>Read-only view of one order and its lines.</summary>
    public class OrderDetailsViewModel
    {
        public Order Order { get; set; } = null!;

        public List<OrderLineViewModel> Lines { get; set; } = new();
    }
}