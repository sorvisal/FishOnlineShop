namespace FishOnlineShop.Services
{
    /// <summary>
    /// Session backed cart storage. The session only ever holds product ids and
    /// quantities; names, prices and stock are always re-read from the database, so a
    /// tampered payload can never change what the customer is charged.
    /// </summary>
    public interface ICartSession
    {
        /// <summary>Total number of individual units in the cart, used for the navbar badge.</summary>
        int GetItemCount();

        /// <summary>
        /// Current lines, de-duplicated by product id and stripped of any non-positive
        /// id or quantity. Ids and quantities only, never product data.
        /// </summary>
        IReadOnlyList<CartLine> GetLines();

        /// <summary>
        /// Adds <paramref name="quantity"/> units of a product, merging into the existing
        /// line when the product is already in the cart. The caller is responsible for
        /// clamping against current stock.
        /// </summary>
        void Add(int productId, int quantity);

        /// <summary>Replaces the quantity of a line. A quantity of zero or less removes it.</summary>
        void SetQuantity(int productId, int quantity);

        /// <summary>Removes a single line, leaving the rest of the cart intact.</summary>
        void Remove(int productId);

        /// <summary>Empties the cart.</summary>
        void Clear();
    }
}