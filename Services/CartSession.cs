using System.Text.Json;

namespace FishOnlineShop.Services
{
    /// <summary>
    /// Session backed cart storage. Only product ids and quantities are kept in the
    /// session; product names, prices and stock are always re-read from SQL Server.
    /// </summary>
    public class CartSession : ICartSession
    {
        public const string SessionKey = "FishOnlineShop.Cart";

        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        // ISession is not registered in DI, it is only reachable through HttpContext.Session,
        // so the accessor is what gets injected.
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<CartSession> _logger;

        public CartSession(IHttpContextAccessor httpContextAccessor, ILogger<CartSession> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        private ISession? Session => _httpContextAccessor.HttpContext?.Session;

        public int GetItemCount()
        {
            var lines = Read();
            return lines.Sum(line => line.Quantity);
        }

        public IReadOnlyList<CartLine> GetLines() => Read();

        public void Add(int productId, int quantity)
        {
            if (productId <= 0 || quantity <= 0)
            {
                return;
            }

            var lines = Read().ToList();
            var existing = lines.FirstOrDefault(line => line.ProductId == productId);
            if (existing is not null)
            {
                // Guard against overflow on a crafted session payload.
                existing.Quantity = existing.Quantity > int.MaxValue - quantity
                    ? int.MaxValue
                    : existing.Quantity + quantity;
            }
            else
            {
                lines.Add(new CartLine { ProductId = productId, Quantity = quantity });
            }

            Write(lines);
        }

        public void SetQuantity(int productId, int quantity)
        {
            if (productId <= 0)
            {
                return;
            }

            if (quantity <= 0)
            {
                Remove(productId);
                return;
            }

            var lines = Read().ToList();
            var existing = lines.FirstOrDefault(line => line.ProductId == productId);
            if (existing is not null)
            {
                existing.Quantity = quantity;
                Write(lines);
            }
        }

        public void Remove(int productId)
        {
            if (productId <= 0)
            {
                return;
            }

            var lines = Read().Where(line => line.ProductId != productId).ToList();
            Write(lines);
        }

        public void Clear()
        {
            Session?.Remove(SessionKey);
        }

        /// <summary>
        /// Reads the cart from the session. A corrupt or tampered payload is discarded
        /// rather than thrown, so a bad session can never break a page render.
        /// </summary>
        private List<CartLine> Read()
        {
            var session = Session;
            if (session is null || !session.IsAvailable)
            {
                return new List<CartLine>();
            }

            var raw = session.GetString(SessionKey);
            if (string.IsNullOrWhiteSpace(raw))
            {
                return new List<CartLine>();
            }

            try
            {
                var lines = JsonSerializer.Deserialize<List<CartLine>>(raw, SerializerOptions);
                if (lines is null)
                {
                    return new List<CartLine>();
                }

                // Drop anything nonsensical (non-positive ids or quantities) defensively,
                // and merge duplicate ids so a line can never be updated in isolation.
                return lines
                    .Where(line => line is not null && line.ProductId > 0 && line.Quantity > 0)
                    .GroupBy(line => line.ProductId)
                    .Select(group => new CartLine
                    {
                        ProductId = group.Key,
                        Quantity = group.Sum(line => line.Quantity)
                    })
                    .ToList();
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Discarding unreadable cart session payload.");
                session.Remove(SessionKey);
                return new List<CartLine>();
            }
        }

        private void Write(IReadOnlyCollection<CartLine> lines)
        {
            var session = Session;
            if (session is null || !session.IsAvailable)
            {
                _logger.LogWarning("Cart change ignored because no session is available.");
                return;
            }

            if (lines.Count == 0)
            {
                session.Remove(SessionKey);
                return;
            }

            session.SetString(SessionKey, JsonSerializer.Serialize(lines, SerializerOptions));
        }
    }
}