using System;
using System.Collections.Generic;

namespace JeweleryStore1.Models;

public partial class OrderStatusHistory
{
    public int HistoryId { get; set; }

    public int OrderId { get; set; }

    public byte? OldStatus { get; set; }

    public byte NewStatus { get; set; }

    public int? ChangedBy { get; set; }

    public string? Notes { get; set; }

    public DateTime ChangedAt { get; set; }

    public virtual Order Order { get; set; } = null!;
}
