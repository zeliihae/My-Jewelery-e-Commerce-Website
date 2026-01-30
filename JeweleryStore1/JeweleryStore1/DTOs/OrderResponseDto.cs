namespace JeweleryStore1.DTOs
{
    public class OrderResponseDto
    {
        public int OrderId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; }
        public decimal OrderTotal { get; set; }
        public string OrderStatus { get; set; }
        public DateTime? OrderCreatedAt { get; set; }
        public string PaymentMethod { get; set; }
        public string TrackingNumber { get; set; }
        public List<OrderItemResponseDto> Items { get; set; }
    }

    public class OrderItemResponseDto
    {
        public int OrderItemId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string ProductImage { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal? Discount { get; set; }
        public decimal Subtotal { get; set; }
    }
}