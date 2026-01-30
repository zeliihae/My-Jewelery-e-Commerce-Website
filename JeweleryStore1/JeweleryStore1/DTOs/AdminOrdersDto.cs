
public class OrderStatusUpdateDto
{
    public string NewStatus { get; set; } = null!; // "pending", "processing", "shipped", "delivered", "cancelled"
    public string? Notes { get; set; }
}

public class PaymentStatusUpdateDto
{
    public byte PaymentStatus { get; set; } // 0 = Pending, 1 = Paid
}

public class OrderUpdateDto
{
    public string OrderStatus { get; set; } = null!;
    public string? PaymentMethod { get; set; }
    public byte PaymentStatus { get; set; }
    public string? Notes { get; set; }
}

public class BulkStatusUpdateDtoAd
{
    public List<int> OrderIds { get; set; } = new();
    public string NewStatus { get; set; } = null!;
    public string? Notes { get; set; }
}

