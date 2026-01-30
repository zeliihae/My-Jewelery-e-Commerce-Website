using System;
using System.Collections.Generic;

namespace JeweleryStore1.Models;

public partial class Order
{
    public int OrderId { get; set; }

    public string TrackingNumber { get; set; } = null!;

    public int UserId { get; set; }

    public DateTime OrderDate { get; set; }

    public byte OrderStatus { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal DiscountAmount { get; set; }

    public int? ShippingAddressId { get; set; }

    public int? BillingAddressId { get; set; }

    public string? PaymentMethod { get; set; }

    public byte PaymentStatus { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public int? CouponId { get; set; }

    public virtual Coupon? Coupon { get; set; }

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual ICollection<OrderStatusHistory> OrderStatusHistories { get; set; } = new List<OrderStatusHistory>();

    public virtual ICollection<Receipt> Receipts { get; set; } = new List<Receipt>();

    public virtual User User { get; set; } = null!;
}
