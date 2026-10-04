namespace FishOnlineShop.Services
{
    /// <summary>
    /// Email is the login identifier, so it is stored in one canonical form. Trimming and
    /// lower casing here means "Bob@Example.com " and "bob@example.com" cannot end up as
    /// two accounts, which also satisfies the unique IX_Users_Email index.
    /// </summary>
    public static class UserEmail
    {
        public static string Normalize(string? email)
            => (email ?? string.Empty).Trim().ToLowerInvariant();

        public static bool IsValid(string? email)
            => !string.IsNullOrWhiteSpace(Normalize(email));
    }
}