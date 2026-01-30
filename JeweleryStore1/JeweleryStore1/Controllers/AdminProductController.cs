using JeweleryStore1.Data;
using JeweleryStore1.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace JeweleryStore1.Controllers.Admin
{
   
    [Route("api/admin/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly JewDbContext _context;

        public ProductsController(JewDbContext context)
        {
            _context = context;
        }



        // GET: api/admin/Products
        [HttpGet]
        public async Task<ActionResult<object>> GetProducts(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? search = null,
            [FromQuery] int? categoryId = null,
            [FromQuery] byte? status = null)
        {
          

            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .AsQueryable();

            // Search
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(p =>
                    p.ProductName.Contains(search) ||
                    (p.ProductDescription != null && p.ProductDescription.Contains(search)));
            }

            // Filter by category
            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            // Filter by status (1 = Active, 0 = Inactive)
            if (status.HasValue)
            {
                query = query.Where(p => p.ProductStatus == status.Value);
            }

            var totalCount = await query.CountAsync();

            var products = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new
                {
                    p.ProductId,
                    p.ProductName,
                    p.ProductDescription,
                    p.ProductPrice,
                    p.ProductDiscountPrice,
                    FinalPrice = p.ProductDiscountPrice ?? p.ProductPrice,
                    p.ProductStock,
                    ProductStatus = p.ProductStatus == 1 ? "Active" : "Inactive",
                    IsActive = p.ProductStatus == 1,
                    p.ProductImage,
                    p.CategoryId,
                    CategoryName = p.Category != null ? p.Category.CategoryName : "Kategori Yok",
                    p.ViewCount,
                    p.CreatedAt,
                    p.UpdatedAt,
                    ImageCount = p.ProductImages.Count,
                    MainImage = p.ProductImages
                        .Where(i => i.IsPrimary)
                        .Select(i => i.ImageUrl)
                        .FirstOrDefault()
                        ?? p.ProductImages
                            .OrderBy(i => i.ImageOrder)
                            .Select(i => i.ImageUrl)
                            .FirstOrDefault()
                        ?? p.ProductImage
                })
                .ToListAsync();

            return Ok(new
            {
                products,
                totalCount,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            });
        }

        // GET: api/admin/Products/5
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetProduct(int id)
        {
         
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .Include(p => p.Reviews)
                .Where(p => p.ProductId == id)
                .Select(p => new
                {
                    p.ProductId,
                    p.ProductName,
                    p.ProductDescription,
                    p.ProductPrice,
                    p.ProductDiscountPrice,
                    FinalPrice = p.ProductDiscountPrice ?? p.ProductPrice,
                    p.ProductStock,
                    p.ProductStatus,
                    IsActive = p.ProductStatus == 1,
                    p.ProductImage,
                    p.CategoryId,
                    CategoryName = p.Category != null ? p.Category.CategoryName : "Kategori Yok",
                    p.ViewCount,
                    p.CreatedAt,
                    p.UpdatedAt,
                    Images = p.ProductImages.OrderBy(i => i.ImageOrder).Select(i => new
                    {
                        i.ImageId,
                        i.ImageUrl,
                        i.ImageOrder,
                        i.IsPrimary,
                        i.CreatedAt
                    }),
                    ReviewCount = p.Reviews.Count,
                    AverageRating = p.Reviews.Any() ? p.Reviews.Average(r => (double)r.Rating) : 0
                })
                .FirstOrDefaultAsync();

            if (product == null)
            {
                return NotFound();
            }

            return product;
        }

        // POST: api/admin/Products
        [HttpPost]
        public async Task<ActionResult<Product>> CreateProduct(ProductCreateDto productDto)
        {
            

            // Validate category exists if provided
            if (productDto.CategoryId.HasValue)
            {
                if (!await _context.Categories.AnyAsync(c => c.CategoryId == productDto.CategoryId.Value))
                {
                    return BadRequest("Kategori bulunamadı");
                }
            }

            var product = new Product
            {
                ProductName = productDto.ProductName,
                ProductDescription = productDto.ProductDescription,
                ProductPrice = productDto.ProductPrice,
                ProductDiscountPrice = productDto.ProductDiscountPrice,
                ProductStock = productDto.ProductStock,
                ProductImage = productDto.ProductImage,
                CategoryId = productDto.CategoryId,
                ProductStatus = 1, // Active by default
                ViewCount = 0,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetProduct), new { id = product.ProductId }, product);
        }

        // PUT: api/admin/Products/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(int id, ProductUpdateDto productDto)
        {
           

            var product = await _context.Products.FindAsync(id);

            if (product == null)
            {
                return NotFound();
            }

            // Validate category exists if provided
            if (productDto.CategoryId.HasValue)
            {
                if (!await _context.Categories.AnyAsync(c => c.CategoryId == productDto.CategoryId.Value))
                {
                    return BadRequest("Kategori bulunamadı");
                }
            }

            product.ProductName = productDto.ProductName;
            product.ProductDescription = productDto.ProductDescription;
            product.ProductPrice = productDto.ProductPrice;
            product.ProductDiscountPrice = productDto.ProductDiscountPrice;
            product.ProductStock = productDto.ProductStock;
            product.ProductImage = productDto.ProductImage;
            product.CategoryId = productDto.CategoryId;
            product.ProductStatus = productDto.ProductStatus;
            product.UpdatedAt = DateTime.Now;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductExists(id))
                {
                    return NotFound();
                }
                throw;
            }

            return NoContent();
        }

        // DELETE: api/admin/Products/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
           

            var product = await _context.Products
                .Include(p => p.ProductImages)
                .Include(p => p.CartItems)
                .Include(p => p.OrderItems)
                .Include(p => p.Favorites)
                .Include(p => p.Reviews)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null)
            {
                return NotFound();
            }

            // Check if product is used in orders
            if (product.OrderItems.Any())
            {
                return BadRequest("Bu ürün siparişlerde kullanıldığı için silinemez. Pasif hale getirebilirsiniz.");
            }

            // Remove related data
            _context.ProductImages.RemoveRange(product.ProductImages);
            _context.CartItems.RemoveRange(product.CartItems);
            _context.Favorites.RemoveRange(product.Favorites);
            _context.Reviews.RemoveRange(product.Reviews);
            _context.Products.Remove(product);

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // PATCH: api/admin/Products/5/stock
        [HttpPatch("{id}/stock")]
        public async Task<IActionResult> UpdateStock(int id, [FromBody] StockUpdateDto stockDto)
        {
          

            var product = await _context.Products.FindAsync(id);

            if (product == null)
            {
                return NotFound();
            }

            product.ProductStock = stockDto.Stock;
            product.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                productId = product.ProductId,
                productStock = product.ProductStock,
                message = "Stok güncellendi"
            });
        }

        // PATCH: api/admin/Products/5/toggle-status
        [HttpPatch("{id}/toggle-status")]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            

            var product = await _context.Products.FindAsync(id);

            if (product == null)
            {
                return NotFound();
            }

            product.ProductStatus = (byte)(product.ProductStatus == 1 ? 0 : 1);
            product.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                productId = product.ProductId,
                productStatus = product.ProductStatus,
                isActive = product.ProductStatus == 1,
                message = product.ProductStatus == 1 ? "Ürün aktif edildi" : "Ürün pasif edildi"
            });
        }

        // PATCH: api/admin/Products/5/discount
        [HttpPatch("{id}/discount")]
        public async Task<IActionResult> UpdateDiscount(int id, [FromBody] DiscountUpdateDto discountDto)
        {
      

            var product = await _context.Products.FindAsync(id);

            if (product == null)
            {
                return NotFound();
            }

            if (discountDto.ProductDiscountPrice.HasValue && discountDto.ProductDiscountPrice.Value >= product.ProductPrice)
            {
                return BadRequest("İndirimli fiyat, normal fiyattan düşük olmalıdır");
            }

            product.ProductDiscountPrice = discountDto.ProductDiscountPrice;
            product.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                productId = product.ProductId,
                productPrice = product.ProductPrice,
                productDiscountPrice = product.ProductDiscountPrice,
                finalPrice = product.ProductDiscountPrice ?? product.ProductPrice,
                message = discountDto.ProductDiscountPrice.HasValue ? "İndirim uygulandı" : "İndirim kaldırıldı"
            });
        }

        // POST: api/admin/Products/5/images
        [HttpPost("{id}/images")]
        public async Task<ActionResult<ProductImage>> AddProductImage(int id, ProductImageDto imageDto)
        {
            

            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            // Eğer bu ilk resimse veya isPrimary true ise, diğer resimlerin isPrimary'sini false yap
            if (imageDto.IsPrimary)
            {
                var existingImages = await _context.ProductImages
                    .Where(pi => pi.ProductId == id)
                    .ToListAsync();

                foreach (var img in existingImages)
                {
                    img.IsPrimary = false;
                }
            }

            // Eğer imageOrder belirtilmemişse, en son sırayı al
            var maxOrder = await _context.ProductImages
                .Where(pi => pi.ProductId == id)
                .MaxAsync(pi => (int?)pi.ImageOrder) ?? 0;

            var productImage = new ProductImage
            {
                ProductId = id,
                ImageUrl = imageDto.ImageUrl,
                ImageOrder = imageDto.ImageOrder ?? (maxOrder + 1),
                IsPrimary = imageDto.IsPrimary,
                CreatedAt = DateTime.Now
            };

            _context.ProductImages.Add(productImage);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetProduct), new { id = product.ProductId }, productImage);
        }

        // PUT: api/admin/Products/images/5
        [HttpPut("images/{imageId}")]
        public async Task<IActionResult> UpdateProductImage(int imageId, ProductImageUpdateDto imageDto)
        {
           

            var image = await _context.ProductImages.FindAsync(imageId);
            if (image == null)
            {
                return NotFound();
            }

            // Eğer isPrimary true yapılıyorsa, aynı üründeki diğer resimlerin isPrimary'sini false yap
            if (imageDto.IsPrimary && !image.IsPrimary)
            {
                var existingImages = await _context.ProductImages
                    .Where(pi => pi.ProductId == image.ProductId && pi.ImageId != imageId)
                    .ToListAsync();

                foreach (var img in existingImages)
                {
                    img.IsPrimary = false;
                }
            }

            image.ImageUrl = imageDto.ImageUrl;
            image.ImageOrder = imageDto.ImageOrder;
            image.IsPrimary = imageDto.IsPrimary;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // PATCH: api/admin/Products/images/5/set-primary
        [HttpPatch("images/{imageId}/set-primary")]
        public async Task<IActionResult> SetPrimaryImage(int imageId)
        {
          

            var image = await _context.ProductImages.FindAsync(imageId);
            if (image == null)
            {
                return NotFound();
            }

            // Aynı üründeki tüm resimlerin isPrimary'sini false yap
            var existingImages = await _context.ProductImages
                .Where(pi => pi.ProductId == image.ProductId)
                .ToListAsync();

            foreach (var img in existingImages)
            {
                img.IsPrimary = (img.ImageId == imageId);
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Ana resim güncellendi", imageId });
        }

        // PATCH: api/admin/Products/images/5/reorder
        [HttpPatch("images/{imageId}/reorder")]
        public async Task<IActionResult> ReorderImage(int imageId, [FromBody] ReorderImageDto reorderDto)
        {
           

            var image = await _context.ProductImages.FindAsync(imageId);
            if (image == null)
            {
                return NotFound();
            }

            image.ImageOrder = reorderDto.NewOrder;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Resim sırası güncellendi", imageId, newOrder = reorderDto.NewOrder });
        }

        // DELETE: api/admin/Products/images/5
        [HttpDelete("images/{imageId}")]
        public async Task<IActionResult> DeleteProductImage(int imageId)
        {
            

            var image = await _context.ProductImages.FindAsync(imageId);
            if (image == null)
            {
                return NotFound();
            }

            _context.ProductImages.Remove(image);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // GET: api/admin/Products/stats
        [HttpGet("stats")]
        public async Task<ActionResult<object>> GetProductStats()
        {
            
            var totalProducts = await _context.Products.CountAsync();
            var activeProducts = await _context.Products.CountAsync(p => p.ProductStatus == 1);
            var inactiveProducts = await _context.Products.CountAsync(p => p.ProductStatus == 0);
            var inStockProducts = await _context.Products.CountAsync(p => p.ProductStock > 0);
            var outOfStockProducts = await _context.Products.CountAsync(p => p.ProductStock == 0);
            var lowStockProducts = await _context.Products.CountAsync(p => p.ProductStock > 0 && p.ProductStock <= 10);
            var discountedProducts = await _context.Products.CountAsync(p => p.ProductDiscountPrice.HasValue);

            var topProducts = await _context.OrderItems
                .GroupBy(oi => oi.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    TotalSold = g.Sum(oi => oi.Quantity),
                    Revenue = g.Sum(oi => oi.Quantity * oi.UnitPrice)
                })
                .OrderByDescending(x => x.TotalSold)
                .Take(5)
                .Join(_context.Products,
                    stat => stat.ProductId,
                    product => product.ProductId,
                    (stat, product) => new
                    {
                        product.ProductId,
                        product.ProductName,
                        product.ProductPrice,
                        product.ProductDiscountPrice,
                        stat.TotalSold,
                        stat.Revenue
                    })
                .ToListAsync();

            var mostViewedProducts = await _context.Products
                .OrderByDescending(p => p.ViewCount)
                .Take(5)
                .Select(p => new
                {
                    p.ProductId,
                    p.ProductName,
                    p.ViewCount,
                    p.ProductPrice
                })
                .ToListAsync();

            var categoryDistribution = await _context.Products
                .GroupBy(p => p.Category)
                .Select(g => new
                {
                    CategoryName = g.Key != null ? g.Key.CategoryName : "Kategorisiz",
                    ProductCount = g.Count(),
                    TotalStock = g.Sum(p => p.ProductStock)
                })
                .OrderByDescending(x => x.ProductCount)
                .ToListAsync();

            var totalInventoryValue = await _context.Products
                .SumAsync(p => p.ProductStock * (p.ProductDiscountPrice ?? p.ProductPrice));

            return Ok(new
            {
                totalProducts,
                activeProducts,
                inactiveProducts,
                inStockProducts,
                outOfStockProducts,
                lowStockProducts,
                discountedProducts,
                totalInventoryValue,
                topProducts,
                mostViewedProducts,
                categoryDistribution
            });
        }

        // POST: api/admin/Products/bulk-update-stock
        [HttpPost("bulk-update-stock")]
        public async Task<IActionResult> BulkUpdateStock([FromBody] List<BulkStockUpdateDto> updates)
        {
        
            var updatedCount = 0;

            foreach (var update in updates)
            {
                var product = await _context.Products.FindAsync(update.ProductId);
                if (product != null)
                {
                    product.ProductStock = update.Stock;
                    product.UpdatedAt = DateTime.Now;
                    updatedCount++;
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = $"{updatedCount} ürün stoku güncellendi" });
        }

        // POST: api/admin/Products/bulk-update-status
        [HttpPost("bulk-update-status")]
        public async Task<IActionResult> BulkUpdateStatus([FromBody] BulkStatusUpdateDto statusDto)
        {
          

            var products = await _context.Products
                .Where(p => statusDto.ProductIds.Contains(p.ProductId))
                .ToListAsync();

            foreach (var product in products)
            {
                product.ProductStatus = statusDto.Status;
                product.UpdatedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = $"{products.Count} ürün durumu güncellendi" });
        }

        // POST: api/admin/Products/bulk-discount
        [HttpPost("bulk-discount")]
        public async Task<IActionResult> BulkApplyDiscount([FromBody] BulkDiscountDto discountDto)
        {
           

            var products = await _context.Products
                .Where(p => discountDto.ProductIds.Contains(p.ProductId))
                .ToListAsync();

            foreach (var product in products)
            {
                if (discountDto.DiscountPercentage.HasValue)
                {
                    var discountAmount = product.ProductPrice * (discountDto.DiscountPercentage.Value / 100);
                    product.ProductDiscountPrice = product.ProductPrice - discountAmount;
                }
                else if (discountDto.DiscountAmount.HasValue)
                {
                    product.ProductDiscountPrice = product.ProductPrice - discountDto.DiscountAmount.Value;
                }
                product.UpdatedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = $"{products.Count} ürüne indirim uygulandı" });
        }

       
 private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.ProductId == id);
        }
    }

   }