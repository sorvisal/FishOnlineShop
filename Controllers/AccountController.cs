using System.Security.Claims;
using FishOnlineShop.Data;
using FishOnlineShop.Models;
using FishOnlineShop.Services;
using FishOnlineShop.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FishOnlineShop.Controllers
{
    public class AccountController : Controller
    {
        /// <summary>
        /// Shown for both an unknown email and a wrong password so the form cannot be used
        /// to discover which addresses have accounts.
        /// </summary>
        private const string GenericLoginError = "Invalid email or password.";

        private const string DuplicateEmailError = "An account with this email address already exists.";

        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            ApplicationDbContext context,
            IPasswordHasher<User> passwordHasher,
            ILogger<AccountController> logger)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _logger = logger;
        }

        // ---------------------------------------------------------------- Register

        [HttpGet]
        public IActionResult Register(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return Redirect(SafeReturnUrl(returnUrl) ?? Url.Action(nameof(Orders))!);
            }

            ViewData["ReturnUrl"] = SafeReturnUrl(returnUrl);
            return View(new RegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model, string? returnUrl)
        {
            var safeReturnUrl = SafeReturnUrl(returnUrl);
            ViewData["ReturnUrl"] = safeReturnUrl;

            if (User.Identity?.IsAuthenticated == true)
            {
                return Redirect(safeReturnUrl ?? Url.Action(nameof(Orders))!);
            }

            model.Email = UserEmail.Normalize(model.Email);

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var emailExists = await _context.Users
                .AsNoTracking()
                .AnyAsync(u => u.Email == model.Email);

            if (emailExists)
            {
                ModelState.AddModelError(nameof(model.Email), DuplicateEmailError);
                return View(model);
            }

            var user = new User
            {
                FullName = model.FullName.Trim(),
                Email = model.Email,
                // Salted PBKDF2 hash; the plaintext is never persisted and never logged.
                PasswordHash = _passwordHasher.HashPassword(new User(), model.Password),
                Role = UserRoles.Customer,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // The unique index is the real guard against two simultaneous registrations.
                ModelState.AddModelError(nameof(model.Email), DuplicateEmailError);
                return View(model);
            }

            await SignInAsync(user);

            TempData["SuccessMessage"] = $"Welcome, {user.FullName}! Your account is ready.";
            _logger.LogInformation("Registered a new customer account for {Email}.", user.Email);

            return Redirect(safeReturnUrl ?? Url.Action(nameof(Orders))!);
        }

        // ------------------------------------------------------------------- Login

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return Redirect(SafeReturnUrl(returnUrl) ?? Url.Action(nameof(Orders))!);
            }

            var safeReturnUrl = SafeReturnUrl(returnUrl);
            ViewData["ReturnUrl"] = safeReturnUrl;
            return View(new LoginViewModel { ReturnUrl = safeReturnUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            ViewData["ReturnUrl"] = model.ReturnUrl;

            if (User.Identity?.IsAuthenticated == true)
            {
                return Redirect(SafeReturnUrl(model.ReturnUrl) ?? Url.Action(nameof(Orders))!);
            }

            model.Email = UserEmail.Normalize(model.Email);

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == model.Email);

            var hasher = _passwordHasher;

            if (user is null)
            {
                // Still run a verification so an unknown email costs roughly the same as a
                // wrong password and cannot be timed apart.
                hasher.VerifyHashedPassword(new User(), DummyHash.Value, model.Password);
                TempData["ErrorMessage"] = GenericLoginError;
                return View(model);
            }

            var verification = hasher.VerifyHashedPassword(user, user.PasswordHash, model.Password);

            if (verification == PasswordVerificationResult.Failed)
            {
                TempData["ErrorMessage"] = GenericLoginError;
                return View(model);
            }

            if (verification == PasswordVerificationResult.SuccessRehashNeeded)
            {
                user.PasswordHash = hasher.HashPassword(user, model.Password);
                _context.Users.Update(user);
                await _context.SaveChangesAsync();
            }

            await SignInAsync(user);

            TempData["SuccessMessage"] = $"Welcome back, {user.FullName}!";

            return Redirect(SafeReturnUrl(model.ReturnUrl) ?? Url.Action(nameof(Orders))!);
        }

        // ------------------------------------------------------------------ Logout

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            TempData["SuccessMessage"] = "You have been signed out.";
            return RedirectToAction("Index", "Home");
        }

        // ------------------------------------------------------------ AccessDenied

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        // ---------------------------------------------------------------- Orders

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Orders()
        {
            var model = new AccountOrdersViewModel
            {
                Orders = await _context.Orders
                    .AsNoTracking()
                    .Where(o => o.UserId == CurrentUserId)
                    .OrderByDescending(o => o.OrderDate)
                    .ThenByDescending(o => o.OrderId)
                    .Select(o => new UserOrderSummaryViewModel
                    {
                        OrderId = o.OrderId,
                        OrderDate = o.OrderDate,
                        TotalAmount = o.TotalAmount,
                        OrderStatus = o.OrderStatus,
                        PaymentMethod = o.PaymentMethod,
                        LineCount = o.OrderDetails.Count()
                    })
                    .ToListAsync()
            };

            return View(model);
        }

        /// <summary>
        /// One order in full. Owner or admin only; anyone else gets 404 so changing the id
        /// in the URL cannot be used to read another customer's order.
        /// </summary>
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> OrderDetails(int id)
        {
            var order = await _context.Orders
                .AsNoTracking()
                .Include(o => o.OrderDetails)
                // Without this the Product navigation property stays null, because the
                // context uses AsNoTracking and has lazy-loading proxies switched off.
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order is null || !CanViewOrder(order.UserId))
            {
                return NotFound();
            }

            return View(new OrderDetailsViewModel
            {
                Order = order,
                // Flattened to plain values so the view cannot dereference a null Product.
                Lines = OrderLineViewModel.FromMany(order.OrderDetails.OrderBy(d => d.OrderDetailId))
            });
        }

        private bool IsAdmin => User.IsInRole(UserRoles.Admin);

        private bool CanViewOrder(int ownerUserId)
            => ownerUserId == CurrentUserId || IsAdmin;

        // ------------------------------------------------------------------ Helpers

        private int CurrentUserId
        {
            get
            {
                var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
                return int.TryParse(raw, out var id) ? id : 0;
            }
        }

        private async Task SignInAsync(User user)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new(ClaimTypes.Name, user.FullName),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Role, user.Role)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal);
        }

        /// <summary>
        /// Only same-origin, non protocol relative paths survive. This is what stops a
        /// crafted ?returnUrl=https://evil.example from turning the login page into an open
        /// redirect. IsLocalUrl alone rejects most of it; the explicit prefix checks make
        /// the intent obvious and cover "//host" and "/\host" style bypasses.
        /// </summary>
        private string? SafeReturnUrl(string? returnUrl)
        {
            if (string.IsNullOrWhiteSpace(returnUrl))
            {
                return null;
            }

            var candidate = returnUrl.Trim();

            if (!candidate.StartsWith('/'))
            {
                return null;
            }

            if (candidate.StartsWith("//", StringComparison.Ordinal) ||
                candidate.StartsWith("/\\", StringComparison.Ordinal))
            {
                return null;
            }

            return Url.IsLocalUrl(candidate) ? candidate : null;
        }

        /// <summary>
        /// A genuine hash of a random password, used only to equalise the timing of a login
        /// attempt for an address that does not exist. It must be a well formed hash,
        /// otherwise the verification throws instead of simply failing.
        /// </summary>
        private static readonly Lazy<string> DummyHash = new(() =>
            new PasswordHasher<User>().HashPassword(new User(), Guid.NewGuid().ToString("N")));
    }
}