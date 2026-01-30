using System.ComponentModel.DataAnnotations;

namespace JeweleryStore1.DTOs
{
    public class AddToCartDto
    {
        [Required]
        public int ProductId { get; set; }

        [Required]
        [Range(1, 5, ErrorMessage = "Miktar 1 ile 5 arasında olmalıdır")]
        public int Quantity { get; set; }
    }
}