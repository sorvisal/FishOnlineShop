using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FishOnlineShop.Models
{
    public class OrderDetail
    {
        [Key]
        public int OrderDetailId { get; set; }

        [Required(ErrorMessage = "Order is required.")]
        public int OrderId { get; set; }

        [Required(ErrorMessage = "Product is required.")]
        public int ProductId { get; set; }

        [Required(ErrorMessage = "Quantity is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
        public int Quantity { get; set; }

        [Required(ErrorMessage = "Unit price is required.")]
        [Column(TypeName = "decimal(10,2)")]
        [Range(0, 99999999.99, ErrorMessage = "Unit price cannot be negative.")]
        [Display(Name = "Unit Price")]
        public decimal UnitPrice { get; set; }

        [Required(ErrorMessage = "Sub total is required.")]
        [Column(TypeName = "decimal(10,2)")]
        [Range(0, 99999999.99, ErrorMessage = "Sub total cannot be negative.")]
        [Display(Name = "Sub Total")]
        public decimal SubTotal { get; set; }

        [ForeignKey(nameof(OrderId))]
        public Order Order { get; set; } = null!;

        [ForeignKey(nameof(ProductId))]
        public FishProduct Product { get; set; } = null!;
    }
}
