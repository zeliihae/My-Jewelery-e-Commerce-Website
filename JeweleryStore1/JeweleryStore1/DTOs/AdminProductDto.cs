
public class ProductCreateDto
{
    public string ProductName { get; set; } = null!;
    public string? ProductDescription { get; set; }
    public decimal ProductPrice { get; set; }
    public decimal? ProductDiscountPrice { get; set; }
    public int ProductStock { get; set; }
    public string? ProductImage { get; set; }
    public int? CategoryId { get; set; }
}

public class ProductUpdateDto
{
    public string ProductName { get; set; } = null!;
    public string? ProductDescription { get; set; }
    public decimal ProductPrice { get; set; }
    public decimal? ProductDiscountPrice { get; set; }
    public int ProductStock { get; set; }
    public string? ProductImage { get; set; }
    public int? CategoryId { get; set; }
    public byte ProductStatus { get; set; } // 1 = Active, 0 = Inactive
}

public class StockUpdateDto
{
    public int Stock { get; set; }
}

public class DiscountUpdateDto
{
    public decimal? ProductDiscountPrice { get; set; }
}

public class ProductImageDto
{
    public string ImageUrl { get; set; } = null!;
    public int? ImageOrder { get; set; }
    public bool IsPrimary { get; set; } = false;
}

public class ProductImageUpdateDto
{
    public string ImageUrl { get; set; } = null!;
    public int ImageOrder { get; set; }
    public bool IsPrimary { get; set; }
}

public class ReorderImageDto
{
    public int NewOrder { get; set; }
}

public class BulkStockUpdateDto
{
    public int ProductId { get; set; }
    public int Stock { get; set; }
}

public class BulkStatusUpdateDto
{
    public List<int> ProductIds { get; set; } = new();
    public byte Status { get; set; } // 1 = Active, 0 = Inactive
}

public class BulkDiscountDto
{
    public List<int> ProductIds { get; set; } = new();
    public decimal? DiscountPercentage { get; set; }
    public decimal? DiscountAmount { get; set; }
}

  