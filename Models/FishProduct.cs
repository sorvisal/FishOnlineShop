using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FishOnlineShop.Models
{
    public class FishProduct
    {
        [Key]
        public int ProductId { get; set; }

        [Required(ErrorMessage = "Category is required.")]
        [Display(Name = "Category")]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Fish name is required.")]
        [StringLength(150, MinimumLength = 2, ErrorMessage = "Fish name must be between 2 and 150 characters.")]
        [Display(Name = "Fish Name")]
        public string FishName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Price is required.")]
        [Range(0.01, 99999999.99, ErrorMessage = "Price must be greater than 0.")]
        [Column(TypeName = "decimal(10,2)")]
        [Display(Name = "Price ($)")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Stock is required.")]
        [Range(0, int.MaxValue, ErrorMessage = "Stock cannot be negative.")]
        [Display(Name = "Stock Quantity")]
        public int Stock { get; set; }

        [Range(0.01, 9999.99, ErrorMessage = "Weight must be greater than 0.")]
        [Column(TypeName = "decimal(6,2)")]
        [Display(Name = "Weight (kg)")]
        public decimal Weight { get; set; }

        [StringLength(255, ErrorMessage = "Image path cannot exceed 255 characters.")]
        [Display(Name = "Image URL")]
        public string? ImageUrl { get; set; }

        [Display(Name = "Description")]
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; }

        [ForeignKey(nameof(CategoryId))]
        public Category Category { get; set; } = null!;

        public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    }
}
