namespace JeweleryStore1.DTOs
{
    // Kategori listesi için response
    public class CategoryResponseDto
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public string? CategoryIcon { get; set; }
        public string? CategoryDescription { get; set; }
        public int ProductCount { get; set; }
    }

    // Kategori detayı için (ürünleriyle birlikte)
    public class CategoryDetailDto
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public string? CategoryIcon { get; set; }
        public string? CategoryDescription { get; set; }
        public int ProductCount { get; set; }
        public List<ProductSummaryDto> Products { get; set; } = new();
    }

    // Kategori oluşturma
    public class CreateCategoryDto
    {
        public string CategoryName { get; set; }
        public string? CategoryIcon { get; set; }
        public string? CategoryDescription { get; set; }
    }

    // Kategori güncelleme
    public class UpdateCategoryDto
    {
        public string CategoryName { get; set; }
        public string? CategoryIcon { get; set; }
        public string? CategoryDescription { get; set; }
    }

    // Ürün özet bilgisi (kategori içinde kullanmak için)
    public class ProductSummaryDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public decimal ProductPrice { get; set; }
        public decimal? ProductDiscountPrice { get; set; }
        public string? ProductImage { get; set; }
        public int ProductStock { get; set; }
    }
}