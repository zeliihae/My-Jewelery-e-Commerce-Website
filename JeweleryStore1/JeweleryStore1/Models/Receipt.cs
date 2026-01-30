using System;
using System.Collections.Generic;

namespace JeweleryStore1.Models;

public partial class Receipt
{
    public int ReceiptId { get; set; }

    public int OrderId { get; set; }

    public string ReceiptNumber { get; set; } = null!;

    public DateTime ReceiptDate { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal TaxAmount { get; set; }

    public byte ReceiptType { get; set; }

    public byte ReceiptStatus { get; set; }

    public string? Notes { get; set; }

    public virtual Order Order { get; set; } = null!;

  

    public virtual ICollection<ReceiptItem> ReceiptItems { get; set; } = new List<ReceiptItem>();
}
