using System;
using System.Collections.Generic;

namespace JeweleryStore1.Models;

public partial class Category
{
    public int CategoryId { get; set; }

    public string CategoryName { get; set; } = null!;

    public string? CategoryIcon { get; set; }

    public string? CategoryDescription { get; set; }

    public byte CategoryStatus { get; set; }

    public int DisplayOrder { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
