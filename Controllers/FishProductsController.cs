using FishOnlineShop.Data;
using FishOnlineShop.Models;
using FishOnlineShop.Services;
using FishOnlineShop.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FishOnlineShop.Controllers
{
    /// <summary>
    /// Admin-only catalogue management. Applied to the whole controller rather than just
    /// Create/Edit/Delete because Index is the "browse and manage every fish" admin screen
    /// and Details is only reachable from it. The public storefront lives in
    /// <see cref="ShopController"/>, which is unaffected.
    /// </summary>
    [Authorize(Roles = UserRoles.Admin)]
    public class FishProductsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly FishImageStorage _imageStorage;
        private readonly ILogger<FishProductsController> _logger;

        public FishProductsController(
            ApplicationDbContext context,
            FishImageStorage imageStorage,
            ILogger<FishProductsController> logger)
        {
            _context = context;
            _imageStorage = imageStorage;
            _logger = logger;
        }

        // GET: FishProducts
        public async Task<IActionResult> Index(string? search, int? categoryId)
        {
            var query = _context.FishProducts
                .AsNoTracking()
                .Include(p => p.Category)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(p => p.FishName.Contains(term));
            }

            if (categoryId is > 0)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            var products = await query
                .OrderByDescending(p => p.CreatedAt)
                .ThenBy(p => p.ProductId)
                .ToListAsync();

            ViewBag.Search = search;
            ViewBag.CategoryId = categoryId;
            ViewBag.Categories = await GetCategoryListAsync();

            return View(products);
        }

        // GET: FishProducts/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (!id.HasValue || id.Value <= 0)
            {
                return NotFound();
            }

            var product = await _context.FishProducts
                .AsNoTracking()
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.ProductId == id.Value);

            if (product is null)
            {
                return NotFound();
            }

            return View(product);
        }

        // GET: FishProducts/Create
        public async Task<IActionResult> Create()
        {
            var model = new FishProductCreateViewModel
            {
                Categories = await GetCategoryListAsync()
            };

            return View(model);
        }

        // POST: FishProducts/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FishProductCreateViewModel model, CancellationToken cancellationToken)
        {
            model.Categories = await GetCategoryListAsync();

            // Validation of the upload happens on its own so we can report it inline.
            if (model.ImageFile is not null && model.ImageFile.Length > 0
                && (!FishImageValidator.IsAllowedExtension(model.ImageFile) || !FishImageValidator.IsAllowedSize(model.ImageFile)))
            {
                ModelState.AddModelError(nameof(model.ImageFile), FishImageValidator.BuildErrorMessage());
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var categoryExists = await _context.Categories
                .AsNoTracking()
                .AnyAsync(c => c.CategoryId == model.CategoryId, cancellationToken);

            if (!categoryExists)
            {
                ModelState.AddModelError(nameof(model.CategoryId), "The selected category does not exist.");
                return View(model);
            }

            string? imageUrl = null;
            try
            {
                imageUrl = await _imageStorage.SaveAsync(model.ImageFile, cancellationToken);
            }
            catch (Exception ex) when (ex is InvalidDataException or IOException)
            {
                _logger.LogWarning(ex, "Fish image upload failed.");
                ModelState.AddModelError(nameof(model.ImageFile), "The image could not be uploaded. Please try again.");
                return View(model);
            }

            var product = new FishProduct
            {
                CategoryId = model.CategoryId,
                FishName = model.FishName.Trim(),
                Price = model.Price,
                Stock = model.Stock,
                Weight = model.Weight,
                ImageUrl = imageUrl,
                Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            try
            {
                _context.FishProducts.Add(product);
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Failed to save the new fish product.");
                if (imageUrl is not null)
                {
                    _imageStorage.DeleteIfExists(imageUrl);
                }

                ModelState.AddModelError(string.Empty, "The product could not be saved. Please try again.");
                return View(model);
            }

            TempData["SuccessMessage"] = "Fish product created successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET: FishProducts/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (!id.HasValue || id.Value <= 0)
            {
                return NotFound();
            }

            var product = await _context.FishProducts
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ProductId == id.Value);

            if (product is null)
            {
                return NotFound();
            }

            var model = new FishProductEditViewModel
            {
                ProductId = product.ProductId,
                FishName = product.FishName,
                CategoryId = product.CategoryId,
                Price = product.Price,
                Stock = product.Stock,
                Weight = product.Weight,
                Description = product.Description,
                ImageUrl = product.ImageUrl,
                Categories = await GetCategoryListAsync(product.CategoryId)
            };

            return View(model);
        }

        // POST: FishProducts/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, FishProductEditViewModel model, CancellationToken cancellationToken)
        {
            if (id != model.ProductId)
            {
                return NotFound();
            }

            model.Categories = await GetCategoryListAsync(model.CategoryId);

            if (model.ImageFile is not null && model.ImageFile.Length > 0
                && (!FishImageValidator.IsAllowedExtension(model.ImageFile) || !FishImageValidator.IsAllowedSize(model.ImageFile)))
            {
                ModelState.AddModelError(nameof(model.ImageFile), FishImageValidator.BuildErrorMessage());
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var product = await _context.FishProducts.FirstOrDefaultAsync(p => p.ProductId == id, cancellationToken);
            if (product is null)
            {
                return NotFound();
            }

            var categoryExists = await _context.Categories
                .AsNoTracking()
                .AnyAsync(c => c.CategoryId == model.CategoryId, cancellationToken);

            if (!categoryExists)
            {
                ModelState.AddModelError(nameof(model.CategoryId), "The selected category does not exist.");
                return View(model);
            }

            string? newImageUrl = null;
            try
            {
                newImageUrl = await _imageStorage.SaveAsync(model.ImageFile, cancellationToken);
            }
            catch (Exception ex) when (ex is InvalidDataException or IOException)
            {
                _logger.LogWarning(ex, "Fish image upload failed during edit.");
                ModelState.AddModelError(nameof(model.ImageFile), "The image could not be uploaded. Please try again.");
                return View(model);
            }

            var previousImageUrl = product.ImageUrl;

            product.CategoryId = model.CategoryId;
            product.FishName = model.FishName.Trim();
            product.Price = model.Price;
            product.Stock = model.Stock;
            product.Weight = model.Weight;
            product.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();

            if (newImageUrl is not null)
            {
                product.ImageUrl = newImageUrl;
            }
            else if (model.RemoveImage)
            {
                product.ImageUrl = null;
            }

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Failed to update fish product {ProductId}.", id);
                if (newImageUrl is not null)
                {
                    _imageStorage.DeleteIfExists(newImageUrl);
                }

                ModelState.AddModelError(string.Empty, "The product could not be updated. Please try again.");
                return View(model);
            }

            if (newImageUrl is not null || model.RemoveImage)
            {
                _imageStorage.DeleteIfExists(previousImageUrl);
            }

            TempData["SuccessMessage"] = "Fish product updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET: FishProducts/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (!id.HasValue || id.Value <= 0)
            {
                return NotFound();
            }

            var product = await _context.FishProducts
                .AsNoTracking()
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.ProductId == id.Value);

            if (product is null)
            {
                return NotFound();
            }

            return View(product);
        }

        // POST: FishProducts/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken cancellationToken)
        {
            if (id <= 0)
            {
                return NotFound();
            }

            var product = await _context.FishProducts
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ProductId == id, cancellationToken);

            if (product is null)
            {
                TempData["ErrorMessage"] = "The product you tried to delete no longer exists.";
                return RedirectToAction(nameof(Index));
            }

            var imageUrl = product.ImageUrl;

            try
            {
                _context.FishProducts.Remove(product);
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Failed to delete fish product {ProductId}.", id);
                TempData["ErrorMessage"] = "The product could not be deleted. Please try again.";
                return RedirectToAction(nameof(Index));
            }

            _imageStorage.DeleteIfExists(imageUrl);

            TempData["SuccessMessage"] = "Fish product deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<IEnumerable<SelectListItem>> GetCategoryListAsync(int? selectedCategoryId = null)
        {
            var categories = await _context.Categories
                .AsNoTracking()
                .OrderBy(c => c.CategoryName)
                .Select(c => new SelectListItem
                {
                    Value = c.CategoryId.ToString(),
                    Text = c.CategoryName
                })
                .ToListAsync();

            return categories;
        }
    }
}
