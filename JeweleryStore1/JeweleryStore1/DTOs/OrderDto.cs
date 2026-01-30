using System.ComponentModel.DataAnnotations;

namespace JeweleryStore1.DTOs
{
    public class CreateOrderDto
    {
       

        [Required]
        public int ShippingAddressId { get; set; }

        public int? BillingAddressId { get; set; }

        [Required]
        [StringLength(50)]
        public string PaymentMethod { get; set; } = "Credit cart";// "Kredi Kartı", "Havale", "Kapıda Ödeme"
        public int? CouponId { get; set; }
    }
}