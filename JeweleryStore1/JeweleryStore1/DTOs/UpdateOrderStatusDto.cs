using System.ComponentModel.DataAnnotations;

namespace JeweleryStore1.DTOs
{
    public class UpdateOrderStatusDto
    {
        [Required]
        [StringLength(50)]
        public string OrderStatus { get; set; } // "Pending", "Processing", "Shipped", "Delivered", "Cancelled"
    }
}