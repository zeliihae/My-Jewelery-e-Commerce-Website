using JeweleryStore1.Data;
using JeweleryStore1.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace JeweleryStore1.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class FavoritesController : ControllerBase
    {
        private readonly JewDbContext _context;

        public FavoritesController(JewDbContext context)
        {
            _context = context;
        }

        // GET: api/Favorites
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Favorite>>> GetFavorites()
        {
            var userId = GetCurrentUserId();

            return await _context.Favorites
                .Include(f => f.Product)
                    .ThenInclude(p => p.ProductImages)
                .Include(f => f.Product)
                    .ThenInclude(p => p.Category)
                .Where(f => f.UserId == userId)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();
        }

        // GET: api/Favorites/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Favorite>> GetFavorite(int id)
        {
            var userId = GetCurrentUserId();

            var favorite = await _context.Favorites
                .Include(f => f.Product)
                    .ThenInclude(p => p.ProductImages)
                .Include(f => f.Product)
                    .ThenInclude(p => p.Category)
                .FirstOrDefaultAsync(f => f.FavoriteId == id && f.UserId == userId);

            if (favorite == null)
            {
                return NotFound();
            }

            return favorite;
        }

        // GET: api/Favorites/check/5
        [HttpGet("check/{productId}")]
        public async Task<ActionResult<bool>> CheckFavorite(int productId)
        {
            var userId = GetCurrentUserId();

            var exists = await _context.Favorites
                .AnyAsync(f => f.UserId == userId && f.ProductId == productId);

            return exists;
        }

        // POST: api/Favorites
        [HttpPost]
        public async Task<ActionResult<Favorite>> PostFavorite(FavoriteCreateDto favoriteDto)
        {
            var userId = GetCurrentUserId();

            // Check if product exists
            var productExists = await _context.Products.AnyAsync(p => p.ProductId == favoriteDto.ProductId);
            if (!productExists)
            {
                return BadRequest("Product not found");
            }

            // Check if already in favorites
            var existingFavorite = await _context.Favorites
                .FirstOrDefaultAsync(f => f.UserId == userId && f.ProductId == favoriteDto.ProductId);

            if (existingFavorite != null)
            {
                return Conflict("Product already in favorites");
            }

            var favorite = new Favorite
            {
                UserId = userId,
                ProductId = favoriteDto.ProductId,
                CreatedAt = DateTime.Now
            };

            _context.Favorites.Add(favorite);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetFavorite), new { id = favorite.FavoriteId }, favorite);
        }

        // DELETE: api/Favorites/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFavorite(int id)
        {
            var userId = GetCurrentUserId();

            var favorite = await _context.Favorites
                .FirstOrDefaultAsync(f => f.FavoriteId == id && f.UserId == userId);

            if (favorite == null)
            {
                return NotFound();
            }

            _context.Favorites.Remove(favorite);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/Favorites/product/5
        [HttpDelete("product/{productId}")]
        public async Task<IActionResult> DeleteFavoriteByProductId(int productId)
        {
            var userId = GetCurrentUserId();

            var favorite = await _context.Favorites
                .FirstOrDefaultAsync(f => f.UserId == userId && f.ProductId == productId);

            if (favorite == null)
            {
                return NotFound();
            }

            _context.Favorites.Remove(favorite);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // GET: api/Favorites/count
        [HttpGet("count")]
        public async Task<ActionResult<int>> GetFavoritesCount()
        {
            var userId = GetCurrentUserId();

            var count = await _context.Favorites
                .CountAsync(f => f.UserId == userId);

            return count;
        }

        // POST: api/Favorites/toggle
        [HttpPost("toggle")]
        public async Task<ActionResult<object>> ToggleFavorite(FavoriteCreateDto favoriteDto)
        {
            var userId = GetCurrentUserId();

            var existingFavorite = await _context.Favorites
                .FirstOrDefaultAsync(f => f.UserId == userId && f.ProductId == favoriteDto.ProductId);

            if (existingFavorite != null)
            {
                // Remove from favorites
                _context.Favorites.Remove(existingFavorite);
                await _context.SaveChangesAsync();

                return Ok(new { isFavorite = false, message = "Removed from favorites" });
            }
            else
            {
                // Add to favorites
                var productExists = await _context.Products.AnyAsync(p => p.ProductId == favoriteDto.ProductId);
                if (!productExists)
                {
                    return BadRequest("Product not found");
                }

                var favorite = new Favorite
                {
                    UserId = userId,
                    ProductId = favoriteDto.ProductId,
                    CreatedAt = DateTime.Now
                };

                _context.Favorites.Add(favorite);
                await _context.SaveChangesAsync();

                return Ok(new { isFavorite = true, message = "Added to favorites", favoriteId = favorite.FavoriteId });
            }
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                throw new UnauthorizedAccessException("User not authenticated");
            }

            return userId;
        }

        private bool FavoriteExists(int id)
        {
            return _context.Favorites.Any(e => e.FavoriteId == id);
        }
    }
}

  