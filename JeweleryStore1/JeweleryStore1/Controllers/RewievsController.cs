using JeweleryStore1.Data;
using JeweleryStore1.DTOs;
using JeweleryStore1.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JeweleryStore1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ReviewsController : ControllerBase
    {
        private readonly JewDbContext _context;

        public ReviewsController(JewDbContext context)
        {
            _context = context;
        }

        // GET: api/Reviews/product/{productId}
        [HttpGet("product/{productId}")]
        public async Task<ActionResult<IEnumerable<ReviewResponseDto>>> GetProductReviews(int productId)
        {
            try
            {
                var reviews = await _context.Reviews
                    .Include(r => r.User)
                    .Where(r => r.ProductId == productId)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync();

                var reviewResponses = reviews.Select(r => new ReviewResponseDto
                {
                    ReviewId = r.ReviewId,
                    ProductId = r.ProductId,
                    UserId = r.UserId,
                    UserName = r.User?.UserName ?? "Anonim",
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt
                }).ToList();

                return Ok(reviewResponses);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Yorumlar alınırken hata oluştu", error = ex.Message });
            }
        }

        // GET: api/Reviews/user/{userId}
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<ReviewResponseDto>>> GetUserReviews(int userId)
        {
            try
            {
                var reviews = await _context.Reviews
                    .Include(r => r.User)
                    .Include(r => r.Product)
                    .Where(r => r.UserId == userId)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync();

                var reviewResponses = reviews.Select(r => new ReviewResponseDto
                {
                    ReviewId = r.ReviewId,
                    ProductId = r.ProductId,
                    ProductName = r.Product?.ProductName,
                    UserId = r.UserId,
                    UserName = r.User?.UserName ?? "Anonim",
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt
                }).ToList();

                return Ok(reviewResponses);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Yorumlar alınırken hata oluştu", error = ex.Message });
            }
        }

        // GET: api/Reviews/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<ReviewResponseDto>> GetReview(int id)
        {
            try
            {
                var review = await _context.Reviews
                    .Include(r => r.User)
                    .Include(r => r.Product)
                    .FirstOrDefaultAsync(r => r.ReviewId == id);

                if (review == null)
                {
                    return NotFound(new { message = "Yorum bulunamadı" });
                }

                var reviewResponse = new ReviewResponseDto
                {
                    ReviewId = review.ReviewId,
                    ProductId = review.ProductId,
                    ProductName = review.Product?.ProductName,
                    UserId = review.UserId,
                    UserName = review.User?.UserName ?? "Anonim",
                    Rating = review.Rating,
                    Comment = review.Comment,
                    CreatedAt = review.CreatedAt
                };

                return Ok(reviewResponse);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Yorum alınırken hata oluştu", error = ex.Message });
            }
        }

        // POST: api/Reviews
        [HttpPost]
        public async Task<ActionResult<ReviewResponseDto>> CreateReview([FromBody] CreateReviewDto createDto)
        {
            try
            {
                // Ürün var mı kontrol et
                var product = await _context.Products.FindAsync(createDto.ProductId);
                if (product == null)
                {
                    return NotFound(new { message = "Ürün bulunamadı" });
                }

                // Kullanıcı var mı kontrol et
                var user = await _context.Users.FindAsync(createDto.UserId);
                if (user == null)
                {
                    return NotFound(new { message = "Kullanıcı bulunamadı" });
                }

                // Kullanıcı daha önce bu ürünü satın aldı mı? (opsiyonel)
                var hasPurchased = await _context.Orders
                    .Include(o => o.OrderItems)
                    .AnyAsync(o => o.UserId == createDto.UserId
                              && o.OrderItems.Any(oi => oi.ProductId == createDto.ProductId));

                if (!hasPurchased)
                {
                    return BadRequest(new { message = "Bu ürünü satın almadan yorum yapamazsınız" });
                }

                // Kullanıcı daha önce yorum yaptı mı?
                var existingReview = await _context.Reviews
                    .FirstOrDefaultAsync(r => r.UserId == createDto.UserId && r.ProductId == createDto.ProductId);

                if (existingReview != null)
                {
                    return BadRequest(new { message = "Bu ürün için zaten yorum yaptınız" });
                }

                // Rating kontrolü
                if (createDto.Rating < 1 || createDto.Rating > 5)
                {
                    return BadRequest(new { message = "Puan 1-5 arasında olmalıdır" });
                }

                var review = new Review
                {
                    ProductId = createDto.ProductId,
                    UserId = createDto.UserId,
                    Rating = createDto.Rating,
                    Comment = createDto.Comment,
                    CreatedAt = DateTime.Now
                };

                _context.Reviews.Add(review);
                await _context.SaveChangesAsync();

                var reviewResponse = new ReviewResponseDto
                {
                    ReviewId = review.ReviewId,
                    ProductId = review.ProductId,
                    UserId = review.UserId,
                    UserName = user.UserName,
                    Rating = review.Rating,
                    Comment = review.Comment,
                    CreatedAt = review.CreatedAt
                };

                return CreatedAtAction(nameof(GetReview), new { id = review.ReviewId }, reviewResponse);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Yorum eklenirken hata oluştu", error = ex.Message });
            }
        }

        // PUT: api/Reviews/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateReview(int id, [FromBody] UpdateReviewDto updateDto)
        {
            try
            {
                var review = await _context.Reviews.FindAsync(id);

                if (review == null)
                {
                    return NotFound(new { message = "Yorum bulunamadı" });
                }

                // Sadece yorum sahibi güncelleyebilir
                if (review.UserId != updateDto.UserId)
                {
                    return Forbid();
                }

                // Rating kontrolü
                if (updateDto.Rating < 1 || updateDto.Rating > 5)
                {
                    return BadRequest(new { message = "Puan 1-5 arasında olmalıdır" });
                }

                review.Rating = updateDto.Rating;
                review.Comment = updateDto.Comment;

                await _context.SaveChangesAsync();

                return Ok(new { message = "Yorum güncellendi" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Yorum güncellenirken hata oluştu", error = ex.Message });
            }
        }

        // DELETE: api/Reviews/{id}
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteReview(int id, [FromQuery] int userId)
        {
            try
            {
                var review = await _context.Reviews.FindAsync(id);

                if (review == null)
                {
                    return NotFound(new { message = "Yorum bulunamadı" });
                }

                // Sadece yorum sahibi silebilir
                if (review.UserId != userId)
                {
                    return Forbid();
                }

                _context.Reviews.Remove(review);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Yorum silindi" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Yorum silinirken hata oluştu", error = ex.Message });
            }
        }

        // GET: api/Reviews/product/{productId}/stats
        [HttpGet("product/{productId}/stats")]
        public async Task<ActionResult<ReviewStatsDto>> GetProductReviewStats(int productId)
        {
            try
            {
                var reviews = await _context.Reviews
                    .Where(r => r.ProductId == productId)
                    .ToListAsync();

                if (!reviews.Any())
                {
                    return Ok(new ReviewStatsDto
                    {
                        TotalReviews = 0,
                        AverageRating = 0,
                        RatingDistribution = new Dictionary<int, int>
                        {
                            { 5, 0 }, { 4, 0 }, { 3, 0 }, { 2, 0 }, { 1, 0 }
                        }
                    });
                }

                var ratingGroups = reviews
                    .GroupBy(r => (int)r.Rating)
                    .ToDictionary(g => g.Key, g => g.Count());

                var stats = new ReviewStatsDto
                {
                    TotalReviews = reviews.Count,
                    AverageRating = Math.Round(reviews.Average(r => r.Rating), 1),
                    RatingDistribution = ratingGroups
                };

                // Eksik rating'leri 0 olarak ekle
                for (int i = 1; i <= 5; i++)
                {
                    if (!stats.RatingDistribution.ContainsKey(i))
                    {
                        stats.RatingDistribution[i] = 0;
                    }
                }

                return Ok(stats);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "İstatistikler alınırken hata oluştu", error = ex.Message });
            }
        }
    }
}

