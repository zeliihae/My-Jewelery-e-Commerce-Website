using System;
using System.Collections.Generic;

namespace JeweleryStore1.Models;

public partial class Review
{
    public int ReviewId { get; set; }

    public int ProductId { get; set; }

    public int UserId { get; set; }

    public byte Rating { get; set; }

    public string? Comment { get; set; }

    public byte ReviewStatus { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Product Product { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
