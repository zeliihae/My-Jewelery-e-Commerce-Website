using System;
using System.Collections.Generic;

namespace JeweleryStore1.Models;

public partial class Coupon
{
    public int CouponId { get; set; }

    public string CouponCode { get; set; } = null!;

    public byte CouponType { get; set; }

    public decimal DiscountValue { get; set; }

    public decimal MinOrderAmount { get; set; }

    public decimal? MaxDiscount { get; set; }

    public int? UsageLimit { get; set; }

    public int UsedCount { get; set; }

    public DateTime ValidFrom { get; set; }

    public DateTime? ValidUntil { get; set; }

    public byte CouponStatus { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
