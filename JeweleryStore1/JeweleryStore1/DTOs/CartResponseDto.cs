namespace JeweleryStore1.DTOs
{
    public class CartResponseDto
    {
        public int CartId { get; set; }
        public int UserId { get; set; }
        public List<CartItemDto> Items { get; set; }
        public decimal TotalPrice { get; set; }
        public int TotalItems { get; set; }
    }

    public class CartItemDto
    {
        public int CartItemId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string ProductImage { get; set; }
        public decimal ProductPrice { get; set; }
        public decimal? ProductDiscountPrice { get; set; }
        public int Quantity { get; set; }
        public decimal Subtotal { get; set; }
    }
}
