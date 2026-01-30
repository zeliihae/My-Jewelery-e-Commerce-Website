using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JeweleryStore1.Data;
using JeweleryStore1.Models;
using JeweleryStore1.Models.Responses;
using JeweleryStore1.Exceptions;
using JeweleryStore1.DTOs;

namespace JeweleryStore1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly JewDbContext _context;
        private readonly ILogger<CategoriesController> _logger;

        public CategoriesController(JewDbContext context, ILogger<CategoriesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/Categories
        [HttpGet]
        public async Task<ActionResult<ApiResponse<IEnumerable<CategoryResponseDto>>>> GetAllCategories()
        {
            var categories = await _context.Categories
                .Include(c => c.Products)
                .OrderBy(c => c.CategoryName)
                .ToListAsync();

            var categoryResponses = categories.Select(c => new CategoryResponseDto
            {
                CategoryId = c.CategoryId,
                CategoryName = c.CategoryName,
                CategoryIcon = c.CategoryIcon,
                CategoryDescription = c.CategoryDescription,
                ProductCount = c.Products.Count
            }).ToList();

            return Ok(ApiResponse<IEnumerable<CategoryResponseDto>>.SuccessResponse(
                categoryResponses,
                $"{categoryResponses.Count} kategori listelendi"
            ));
        }

        // GET: api/Categories/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<CategoryDetailDto>>> GetCategory(int id)
        {
            var category = await _context.Categories
                .Include(c => c.Products)
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (category == null)
            {
                throw new NotFoundException("Kategori", id);
            }

            var categoryDetail = new CategoryDetailDto
            {
                CategoryId = category.CategoryId,
                CategoryName = category.CategoryName,
                CategoryIcon = category.CategoryIcon,
                CategoryDescription = category.CategoryDescription,
                ProductCount = category.Products.Count,
                Products = category.Products.Select(p => new ProductSummaryDto
                {
                    ProductId = p.ProductId,
                    ProductName = p.ProductName,
                    ProductPrice = p.ProductPrice,
                    ProductDiscountPrice = p.ProductDiscountPrice,
                    ProductImage = p.ProductImage,
                    ProductStock = p.ProductStock
                }).ToList()
            };

            return Ok(ApiResponse<CategoryDetailDto>.SuccessResponse(
                categoryDetail,
                "Kategori detayları getirildi"
            ));
        }

        // GET: api/Categories/{id}/products
        [HttpGet("{id}/products")]
        public async Task<ActionResult<ApiResponse<IEnumerable<ProductSummaryDto>>>> GetCategoryProducts(int id)
        {
            var category = await _context.Categories
                .Include(c => c.Products)
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (category == null)
            {
                throw new NotFoundException("Kategori", id);
            }

            var products = category.Products.Select(p => new ProductSummaryDto
            {
                ProductId = p.ProductId,
                ProductName = p.ProductName,
                ProductPrice = p.ProductPrice,
                ProductDiscountPrice = p.ProductDiscountPrice,
                ProductImage = p.ProductImage,
                ProductStock = p.ProductStock
            }).ToList();

            return Ok(ApiResponse<IEnumerable<ProductSummaryDto>>.SuccessResponse(
                products,
                $"{products.Count} ürün bulundu"
            ));
        }

        // POST: api/Categories
        [HttpPost]
        public async Task<ActionResult<ApiResponse<CategoryResponseDto>>> CreateCategory([FromBody] CreateCategoryDto createDto)
        {
            // Aynı isimde kategori var mı kontrol et
            var existingCategory = await _context.Categories
                .FirstOrDefaultAsync(c => c.CategoryName.ToLower() == createDto.CategoryName.ToLower());

            if (existingCategory != null)
            {
                throw new BusinessRuleException("Bu isimde bir kategori zaten mevcut");
            }

            var category = new Category
            {
                CategoryName = createDto.CategoryName,
                CategoryIcon = createDto.CategoryIcon,
                CategoryDescription = createDto.CategoryDescription
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Category created: CategoryId={CategoryId}, Name={Name}",
                category.CategoryId, category.CategoryName);

            var response = new CategoryResponseDto
            {
                CategoryId = category.CategoryId,
                CategoryName = category.CategoryName,
                CategoryIcon = category.CategoryIcon,
                CategoryDescription = category.CategoryDescription,
                ProductCount = 0
            };

            return CreatedAtAction(
                nameof(GetCategory),
                new { id = category.CategoryId },
                ApiResponse<CategoryResponseDto>.SuccessResponse(
                    response,
                    "Kategori başarıyla oluşturuldu"
                )
            );
        }

        // PUT: api/Categories/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse>> UpdateCategory(int id, [FromBody] UpdateCategoryDto updateDto)
        {
            var category = await _context.Categories.FindAsync(id);

            if (category == null)
            {
                throw new NotFoundException("Kategori", id);
            }

            // Başka bir kategoride aynı isim var mı kontrol et
            var existingCategory = await _context.Categories
                .FirstOrDefaultAsync(c => c.CategoryName.ToLower() == updateDto.CategoryName.ToLower()
                                       && c.CategoryId != id);

            if (existingCategory != null)
            {
                throw new BusinessRuleException("Bu isimde başka bir kategori zaten mevcut");
            }

            category.CategoryName = updateDto.CategoryName;
            category.CategoryIcon = updateDto.CategoryIcon;
            category.CategoryDescription = updateDto.CategoryDescription;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Category updated: CategoryId={CategoryId}", id);

            return Ok(ApiResponse.SuccessResponse("Kategori güncellendi"));
        }

        // DELETE: api/Categories/{id}
        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse>> DeleteCategory(int id)
        {
            var category = await _context.Categories
                .Include(c => c.Products)
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (category == null)
            {
                throw new NotFoundException("Kategori", id);
            }

            // Kategoride ürün varsa silinmesin
            if (category.Products.Any())
            {
                throw new BusinessRuleException(
                    $"Bu kategoriye ait {category.Products.Count} ürün bulunuyor. Önce ürünleri silin veya başka kategoriye taşıyın"
                );
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Category deleted: CategoryId={CategoryId}", id);

            return Ok(ApiResponse.SuccessResponse("Kategori silindi"));
        }

        // GET: api/Categories/search?name=...
        [HttpGet("search")]
        public async Task<ActionResult<ApiResponse<IEnumerable<CategoryResponseDto>>>> SearchCategories([FromQuery] string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return BadRequest(ApiResponse<IEnumerable<CategoryResponseDto>>.ErrorResponse(
                    "Arama terimi boş olamaz"
                ));
            }

            var categories = await _context.Categories
                .Include(c => c.Products)
                .Where(c => c.CategoryName.Contains(name))
                .OrderBy(c => c.CategoryName)
                .ToListAsync();

            var categoryResponses = categories.Select(c => new CategoryResponseDto
            {
                CategoryId = c.CategoryId,
                CategoryName = c.CategoryName,
                CategoryIcon = c.CategoryIcon,
                CategoryDescription = c.CategoryDescription,
                ProductCount = c.Products.Count
            }).ToList();

            return Ok(ApiResponse<IEnumerable<CategoryResponseDto>>.SuccessResponse(
                categoryResponses,
                $"'{name}' için {categoryResponses.Count} sonuç bulundu"
            ));
        }
    }
}