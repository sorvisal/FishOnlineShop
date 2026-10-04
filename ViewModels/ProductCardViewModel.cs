using FishOnlineShop.Models;

namespace FishOnlineShop.ViewModels
{
    /// <summary>
    /// Input for <c>Shared/_ProductCard.cshtml</c>. The card has to link somewhere
    /// different depending on where it is rendered (storefront vs. admin), so the
    /// target controller/action travel with the model instead of being hard coded
    /// in the partial.
    /// </summary>
    public class ProductCardViewModel
    {
        public FishProduct Product { get; set; } = null!;

        /// <summary>Controller that owns the details page. Defaults to the storefront.</summary>
        public string ControllerName { get; set; } = "Shop";

        public string ActionName { get; set; } = "Details";

        /// <summary>Route values merged into the card link (normally just the product id).</summary>
        public Dictionary<string, string?> RouteValues { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>Bootstrap column classes so callers can control the grid density.</summary>
        public string ColumnClass { get; set; } = "col-12 col-6 col-lg-4 col-xl-3";

        /// <summary>Shows the Edit / Delete buttons used by the admin catalogue.</summary>
        public bool ShowAdminActions { get; set; }

        /// <summary>Label of the primary button at the bottom of the card.</summary>
        public string ActionLabel { get; set; } = "View details";
    }
}