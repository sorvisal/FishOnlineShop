using FishOnlineShop.Data;
using FishOnlineShop.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FishOnlineShop.Services
{
    /// <summary>
    /// One-time creation of the first administrator. Credentials are never compiled in:
    /// they are read from configuration, which means user-secrets in development or an
    /// environment variable anywhere else. If the settings are absent the seeder does
    /// nothing, and if an admin already exists it does nothing, so restarting the app is
    /// always safe.
    /// </summary>
    public class AdminSeeder : IHostedService
    {
        public const string EmailSetting = "AdminSeed:Email";
        public const string PasswordSetting = "AdminSeed:Password";
        public const string FullNameSetting = "AdminSeed:FullName";

        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AdminSeeder> _logger;

        public AdminSeeder(
            IServiceProvider serviceProvider,
            IConfiguration configuration,
            ILogger<AdminSeeder> logger)
        {
            _serviceProvider = serviceProvider;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var email = UserEmail.Normalize(_configuration[EmailSetting]);
            var password = _configuration[PasswordSetting];

            if (!UserEmail.IsValid(email) || string.IsNullOrWhiteSpace(password))
            {
                _logger.LogInformation(
                    "Admin seed skipped: set {Email} and {Password} (user-secrets or environment variables) to create the first admin.",
                    EmailSetting,
                    PasswordSetting);
                return;
            }

            if (password.Length < 8)
            {
                // Never log the value itself.
                _logger.LogWarning("Admin seed skipped: {Setting} must be at least 8 characters.", PasswordSetting);
                return;
            }

            var fullName = _configuration[FullNameSetting];
            if (string.IsNullOrWhiteSpace(fullName))
            {
                fullName = "Administrator";
            }

            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            if (await context.Users.AnyAsync(u => u.Role == UserRoles.Admin, cancellationToken))
            {
                _logger.LogInformation("Admin seed skipped: an admin account already exists.");
                return;
            }

            if (await context.Users.AnyAsync(u => u.Email == email, cancellationToken))
            {
                _logger.LogWarning(
                    "Admin seed skipped: {Email} already belongs to a non-admin account.", EmailSetting);
                return;
            }

            // HashPassword produces a salted PBKDF2 hash; the plaintext is never stored
            // and never logged.
            var user = new User
            {
                FullName = fullName.Trim(),
                Email = email,
                PasswordHash = new PasswordHasher<User>().HashPassword(new User(), password),
                Role = UserRoles.Admin,
                CreatedAt = DateTime.UtcNow
            };

            context.Users.Add(user);

            try
            {
                await context.SaveChangesAsync(cancellationToken);
                _logger.LogWarning("Created the first Admin account for {Email}.", email);
            }
            catch (DbUpdateException ex)
            {
                // A concurrent start may have won the race; that is not a fatal error.
                _logger.LogWarning(ex, "Admin seed skipped: the account could not be created.");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}