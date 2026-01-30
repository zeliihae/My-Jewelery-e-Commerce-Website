using System;
using System.Collections.Generic;

namespace JeweleryStore1.Models;

public partial class OrderSummary
{
    public int OrderId { get; set; }

    public string OrderNumber { get; set; } = null!;

    public string UserName { get; set; } = null!;

    public string UserEmail { get; set; } = null!;

    public DateTime OrderDate { get; set; }

    public byte OrderStatus { get; set; }

    public decimal TotalAmount { get; set; }

    public int? ItemCount { get; set; }
}
