using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JeweleryStore1.Data;
using JeweleryStore1.Models.Responses;

namespace JeweleryStore1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly JewDbContext _context;

        public ProductsController(JewDbContext context)
        {
            _context = context;
        }

        // GET: api/Products?search=yüzük&category=altın
        [HttpGet]
        public async Task<ActionResult<ApiResponse<IEnumerable<object>>>> GetProducts([FromQuery] string? search, [FromQuery] string? category)
        {
            // 1. Sorguyu başlat (Kategorisiyle birlikte çekiyoruz)
            var query = _context.Products
                .Include(p => p.Category)
                .AsQueryable();

            // 2. ARAMA FİLTRESİ: Eğer search parametresi doluysa
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToLower();
                query = query.Where(p =>
                    p.ProductName.ToLower().Contains(search) ||
                    p.ProductDescription.ToLower().Contains(search) ||
                    p.Category.CategoryName.ToLower().Contains(search));
            }

            // 3. KATEGORİ FİLTRESİ: (Eğer category parametresi de varsa)
            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(p => p.Category.CategoryName == category);
            }

            // 4. Veriyi seç ve DTO formatına getir
            var products = await query.Select(p => new
            {
                p.ProductId,
                p.ProductName,
                p.ProductDescription,
                p.ProductPrice,
                p.ProductDiscountPrice,
                p.ProductImage,
                p.ProductStock,
                CategoryName = p.Category.CategoryName
            }).ToListAsync();

            return Ok(ApiResponse<IEnumerable<object>>.SuccessResponse(products, "Ürünler başarıyla getirildi"));
        }
    }
}