namespace FishOnlineShop.Services
{
    /// <summary>
    /// One line in the session backed cart. The cart is intentionally a snapshot of
    /// identifiers only: prices and stock are always re-read from the database.
    /// </summary>
    public class CartLine
    {
        public int ProductId { get; set; }

        public int Quantity { get; set; }
    }
}