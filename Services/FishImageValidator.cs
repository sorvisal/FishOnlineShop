namespace FishOnlineShop.Services
{
    public static class FishImageValidator
    {
        public const long MaxFileSizeBytes = 4 * 1024 * 1024; // 4 MB

        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".webp"
        };

        public static readonly string[] AllowedExtensionsForClient = { ".jpg", ".jpeg", ".png", ".webp" };

        public static bool IsAllowedExtension(IFormFile? file)
            => file is not null && AllowedExtensions.Contains(Path.GetExtension(file.FileName));

        public static bool IsAllowedSize(IFormFile? file)
            => file is not null && file.Length <= MaxFileSizeBytes;

        public static string BuildErrorMessage()
            => "Image must be a .jpg, .jpeg, .png or .webp file no larger than 4 MB.";
    }
}
