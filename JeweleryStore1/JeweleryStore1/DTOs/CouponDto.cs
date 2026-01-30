
public class CouponValidationRequest
{
    public string CouponCode { get; set; } = null!;
    public decimal OrderAmount { get; set; }
}

public class CouponValidationResult
{
    public bool IsValid { get; set; }
    public string Message { get; set; } = null!;
    public int? CouponId { get; set; }
    public decimal DiscountAmount { get; set; }
    public string? DiscountType { get; set; }
    public decimal? DiscountValue { get; set; }
    public decimal FinalAmount { get; set; }
}

public class CouponApplicationRequest
{
    public string CouponCode { get; set; } = null!;
    public decimal OrderAmount { get; set; }
}

public class CouponApplicationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = null!;
    public int? CouponId { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal OriginalAmount { get; set; }
    public decimal FinalAmount { get; set; }
}

public class CouponCreateDto
{
    public string CouponCode { get; set; } = null!;
    public byte CouponType { get; set; } // 0 = Percentage, 1 = Fixed
    public decimal DiscountValue { get; set; }
    public decimal MinOrderAmount { get; set; }
    public decimal? MaxDiscount { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime? ValidUntil { get; set; }
    public int? UsageLimit { get; set; }
}

public class CouponUpdateDto
{
    public string CouponCode { get; set; } = null!;
    public byte CouponType { get; set; } // 0 = Percentage, 1 = Fixed
    public decimal DiscountValue { get; set; }
    public decimal MinOrderAmount { get; set; }
    public decimal? MaxDiscount { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime? ValidUntil { get; set; }
    public int? UsageLimit { get; set; }
    public byte CouponStatus { get; set; } // 1 = Active, 0 = Inactive
}

