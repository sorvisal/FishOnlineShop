using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FishOnlineShop.Models
{
    public class Order
    {
        [Key]
        public int OrderId { get; set; }

        [Required(ErrorMessage = "User is required.")]
        public int UserId { get; set; }

        [Required(ErrorMessage = "Receiver name is required.")]
        [StringLength(100, ErrorMessage = "Receiver name cannot exceed 100 characters.")]
        [Display(Name = "Receiver Name")]
        public string ReceiverName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone is required.")]
        [StringLength(20, ErrorMessage = "Phone cannot exceed 20 characters.")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Shipping address is required.")]
        [StringLength(255, ErrorMessage = "Shipping address cannot exceed 255 characters.")]
        [Display(Name = "Shipping Address")]
        public string ShippingAddress { get; set; } = string.Empty;

        [Required(ErrorMessage = "Payment method is required.")]
        [StringLength(50, ErrorMessage = "Payment method cannot exceed 50 characters.")]
        [Display(Name = "Payment Method")]
        public string PaymentMethod { get; set; } = string.Empty;

        [Display(Name = "Order Date")]
        public DateTime OrderDate { get; set; }

        [Required(ErrorMessage = "Total amount is required.")]
        [Column(TypeName = "decimal(10,2)")]
        [Range(0, 99999999.99, ErrorMessage = "Total amount cannot be negative.")]
        [Display(Name = "Total Amount")]
        public decimal TotalAmount { get; set; }

        [Required(ErrorMessage = "Order status is required.")]
        [StringLength(30, ErrorMessage = "Order status cannot exceed 30 characters.")]
        [Display(Name = "Order Status")]
        public string OrderStatus { get; set; } = "Pending";

        [ForeignKey(nameof(UserId))]
        public User User { get; set; } = null!;

        public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    }
}
