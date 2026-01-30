using System;
using System.Collections.Generic;

namespace JeweleryStore1.Models;

public partial class Product
{
    public int ProductId { get; set; }

    public string ProductName { get; set; } = null!;

    public string? ProductDescription { get; set; }

    public decimal ProductPrice { get; set; }

    public decimal? ProductDiscountPrice { get; set; }

    public int ProductStock { get; set; }

    public string? ProductImage { get; set; }

    public int? CategoryId { get; set; }

    public byte ProductStatus { get; set; }

    public int ViewCount { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

    public virtual Category? Category { get; set; }

    public virtual ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual ICollection<ProductImage> ProductImages { get; set; } = new List<ProductImage>();

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
}
