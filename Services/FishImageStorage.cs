namespace FishOnlineShop.Services
{
    public class FishImageStorage
    {
        public const string UploadFolder = "images/fish";

        private readonly IWebHostEnvironment _environment;

        public FishImageStorage(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        /// <summary>
        /// Validates the upload and saves it under wwwroot/images/fish using a GUID file name.
        /// Returns the relative URL to persist in FishProduct.ImageUrl, or null when nothing was stored.
        /// </summary>
        public async Task<string?> SaveAsync(IFormFile? file, CancellationToken cancellationToken = default)
        {
            if (file is null || file.Length == 0)
            {
                return null;
            }

            if (!FishImageValidator.IsAllowedExtension(file))
            {
                throw new InvalidDataException("Unsupported image file type.");
            }

            if (!FishImageValidator.IsAllowedSize(file))
            {
                throw new InvalidDataException("Image file is too large.");
            }

            // Never trust the client supplied file name: only keep a known-good extension.
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var fileName = $"{Guid.NewGuid()}{extension}";

            var uploadsFolder = Path.Combine(_environment.WebRootPath ?? "wwwroot", UploadFolder);
            Directory.CreateDirectory(uploadsFolder);

            var filePath = Path.Combine(uploadsFolder, fileName);

            await using (var stream = System.IO.File.Create(filePath))
            {
                await file.CopyToAsync(stream, cancellationToken);
            }

            return $"/{UploadFolder}/{fileName}";
        }

        public void DeleteIfExists(string? relativeUrl)
        {
            if (string.IsNullOrWhiteSpace(relativeUrl))
            {
                return;
            }

            var relative = relativeUrl.TrimStart('~', '/').Replace('/', Path.DirectorySeparatorChar);
            if (string.IsNullOrWhiteSpace(relative))
            {
                return;
            }

            var webRoot = _environment.WebRootPath ?? "wwwroot";
            var fullPath = Path.GetFullPath(Path.Combine(webRoot, relative));

            // Guard against path traversal.
            if (!fullPath.StartsWith(Path.GetFullPath(webRoot), StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }
        }
    }
}
