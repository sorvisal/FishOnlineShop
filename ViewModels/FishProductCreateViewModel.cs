using System.ComponentModel.DataAnnotations;
using FishOnlineShop.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FishOnlineShop.ViewModels
{
    public class FishProductCreateViewModel
    {
        [Required(ErrorMessage = "Fish name is required.")]
        [StringLength(150, MinimumLength = 2, ErrorMessage = "Fish name must be between 2 and 150 characters.")]
        [Display(Name = "Fish Name")]
        public string FishName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Category is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a category.")]
        [Display(Name = "Category")]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Price is required.")]
        [Range(0.01, 99999999.99, ErrorMessage = "Price must be greater than 0.")]
        [Display(Name = "Price ($)")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Stock is required.")]
        [Range(0, int.MaxValue, ErrorMessage = "Stock cannot be negative.")]
        [Display(Name = "Stock Quantity")]
        public int Stock { get; set; }

        [Range(0.01, 9999.99, ErrorMessage = "Weight must be greater than 0.")]
        [Display(Name = "Weight (kg)")]
        public decimal Weight { get; set; }

        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Display(Name = "Fish Image")]
        public IFormFile? ImageFile { get; set; }

        public IEnumerable<SelectListItem> Categories { get; set; } = Enumerable.Empty<SelectListItem>();
    }
}
