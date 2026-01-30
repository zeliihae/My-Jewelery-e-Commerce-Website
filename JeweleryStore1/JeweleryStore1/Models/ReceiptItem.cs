using System;
using System.Collections.Generic;

namespace JeweleryStore1.Models;

public partial class ReceiptItem
{
    public int ReceiptItemId { get; set; }

    public int ReceiptId { get; set; }

    public string Description { get; set; } = null!;

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal TotalAmount { get; set; }

    public virtual Receipt Receipt { get; set; } = null!;
}
