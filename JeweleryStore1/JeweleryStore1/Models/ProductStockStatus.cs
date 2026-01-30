using System;
using System.Collections.Generic;

namespace JeweleryStore1.Models;

public partial class ProductStockStatus
{
    public int ProductId { get; set; }

    public string ProductName { get; set; } = null!;

    public string? CategoryName { get; set; }

    public int ProductStock { get; set; }

    public decimal ProductPrice { get; set; }

    public string StockStatus { get; set; } = null!;
}
