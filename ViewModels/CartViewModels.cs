using FishOnlineShop.Models;

namespace FishOnlineShop.ViewModels
{
    /// <summary>
    /// A single cart line, joined to the live product. The price used here always comes
    /// from the database row, never from anything the client sent.
    /// </summary>
    public class CartItemViewModel
    {
        public FishProduct Product { get; set; } = null!;

        public int Quantity { get; set; }

        /// <summary>Highest quantity that may be requested for this line, i.e. current stock.</summary>
        public int MaxQuantity => Math.Max(Product.Stock, 1);

        public decimal UnitPrice => Product.Price;

        public decimal LineTotal => Product.Price * Quantity;

        /// <summary>True when reconciliation had to cut this line down to the available stock.</summary>
        public bool QuantityWasReduced { get; set; }

        /// <summary>
        /// When false the row renders read only, which is what the checkout order summary
        /// needs.
        /// </summary>
        public bool ShowControls { get; set; } = true;
    }

    /// <summary>
    /// Cart lines plus the money summary. Totals are computed by the controller so the
    /// arithmetic never has to live inside a Razor view.
    /// </summary>
    public class CartSummaryViewModel
    {
        public List<CartItemViewModel> Lines { get; set; } = new();

        /// <summary>Sum of all quantities, i.e. the number of units being bought.</summary>
        public int TotalItems { get; set; }

        public decimal SubTotal { get; set; }

        public decimal Shipping { get; set; }

        public decimal Total { get; set; }

        public bool IsEmpty => Lines.Count == 0;
    }

    /// <summary>Data for Cart/Index, which is a summary plus the reconciliation warnings.</summary>
    public class CartIndexViewModel : CartSummaryViewModel
    {
        /// <summary>
        /// Human readable notes about lines that were dropped or trimmed because the
        /// catalogue changed underneath the cart.
        /// </summary>
        public List<string> Adjustments { get; set; } = new();
    }
}