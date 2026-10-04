namespace FishOnlineShop.Models
{
    /// <summary>
    /// The two role values stored in <see cref="User.Role"/>. Kept as constants so the
    /// admin checks in controllers and views never rely on a magic string.
    /// </summary>
    public static class UserRoles
    {
        public const string Admin = "Admin";

        public const string Customer = "Customer";

        public static readonly string[] All = { Admin, Customer };

        public static bool IsValid(string? role)
            => !string.IsNullOrWhiteSpace(role)
               && Array.Exists(All, candidate => string.Equals(candidate, role, StringComparison.OrdinalIgnoreCase));
    }
}