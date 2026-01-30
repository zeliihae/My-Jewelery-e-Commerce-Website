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
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class CouponsController : ControllerBase
    {
        private readonly JewDbContext _context;

        public CouponsController(JewDbContext context)
        {
            _context = context;
        }

        // GET: api/Coupons (Admin only)
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetCoupons()
        {
            var coupons = await _context.Coupons
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new
                {
                    c.CouponId,
                    c.CouponCode,
                    CouponType = c.CouponType == 0 ? "Percentage" : "Fixed",
                    c.DiscountValue,
                    c.MinOrderAmount,
                    c.MaxDiscount,
                    c.UsageLimit,
                    c.UsedCount,
                    c.ValidFrom,
                    c.ValidUntil,
                    CouponStatus = c.CouponStatus == 1 ? "Active" : "Inactive",
                    IsActive = c.CouponStatus == 1,
                    c.CreatedAt
                })
                .ToListAsync();

            return coupons;
        }

        // GET: api/Coupons/5 (Admin only)
        [Authorize(Roles = "Admin")]
        [HttpGet("{id}")]
        public async Task<ActionResult<Coupon>> GetCoupon(int id)
        {
            var coupon = await _context.Coupons.FindAsync(id);

            if (coupon == null)
            {
                return NotFound();
            }

            return coupon;
        }

        // POST: api/Coupons/validate
        [AllowAnonymous]
        [HttpPost("validate")]
        public async Task<ActionResult<CouponValidationResult>> ValidateCoupon(CouponValidationRequest request)
        {
            var coupon = await _context.Coupons
                .FirstOrDefaultAsync(c => c.CouponCode == request.CouponCode.ToUpper());

            if (coupon == null)
            {
                return Ok(new CouponValidationResult
                {
                    IsValid = false,
                    Message = "Kupon kodu bulunamadı"
                });
            }

            // Check if coupon is active (CouponStatus: 1 = Active, 0 = Inactive)
            if (coupon.CouponStatus != 1)
            {
                return Ok(new CouponValidationResult
                {
                    IsValid = false,
                    Message = "Bu kupon artık geçerli değil"
                });
            }

            // Check valid from date
            if (coupon.ValidFrom > DateTime.Now)
            {
                return Ok(new CouponValidationResult
                {
                    IsValid = false,
                    Message = "Bu kupon henüz geçerli değil"
                });
            }

            // Check expiry date
            if (coupon.ValidUntil.HasValue && coupon.ValidUntil.Value < DateTime.Now)
            {
                return Ok(new CouponValidationResult
                {
                    IsValid = false,
                    Message = "Kuponun süresi dolmuş"
                });
            }

            // Check usage limit
            if (coupon.UsageLimit.HasValue && coupon.UsedCount >= coupon.UsageLimit.Value)
            {
                return Ok(new CouponValidationResult
                {
                    IsValid = false,
                    Message = "Kupon kullanım limiti dolmuş"
                });
            }

            // Check minimum order amount
            if (request.OrderAmount < coupon.MinOrderAmount)
            {
                return Ok(new CouponValidationResult
                {
                    IsValid = false,
                    Message = $"Bu kupon en az {coupon.MinOrderAmount:C} tutarındaki siparişler için geçerlidir"
                });
            }

            // Calculate discount
            decimal discountAmount = CalculateDiscount(coupon, request.OrderAmount);

            // Check max discount amount
            if (coupon.MaxDiscount.HasValue && discountAmount > coupon.MaxDiscount.Value)
            {
                discountAmount = coupon.MaxDiscount.Value;
            }

            return Ok(new CouponValidationResult
            {
                IsValid = true,
                Message = "Kupon başarıyla uygulandı",
                CouponId = coupon.CouponId,
                DiscountAmount = discountAmount,
                DiscountType = coupon.CouponType == 0 ? "Percentage" : "Fixed",
                DiscountValue = coupon.DiscountValue,
                FinalAmount = request.OrderAmount - discountAmount
            });
        }

        // POST: api/Coupons/apply
        [Authorize]
        [HttpPost("apply")]
        public async Task<ActionResult<CouponApplicationResult>> ApplyCoupon(CouponApplicationRequest request)
        {
            var userId = GetCurrentUserId();

            // Validate coupon first
            var validationResult = await ValidateCoupon(new CouponValidationRequest
            {
                CouponCode = request.CouponCode,
                OrderAmount = request.OrderAmount
            });

            var validation = (validationResult.Result as OkObjectResult)?.Value as CouponValidationResult;

            if (validation == null || !validation.IsValid)
            {
                return BadRequest(new { message = validation?.Message ?? "Kupon doğrulanamadı" });
            }

            // Check if user has already used this coupon (if one-time use)
            var coupon = await _context.Coupons.FindAsync(validation.CouponId);

            if (coupon != null && coupon.UsageLimit == 1)
            {
                var hasUsed = await _context.Orders
                    .AnyAsync(o => o.UserId == userId && o.CouponId == validation.CouponId);

                if (hasUsed)
                {
                    return BadRequest(new { message = "Bu kuponu daha önce kullandınız" });
                }
            }

            return Ok(new CouponApplicationResult
            {
                Success = true,
                Message = "Kupon uygulandı",
                CouponId = validation.CouponId,
                DiscountAmount = validation.DiscountAmount,
                OriginalAmount = request.OrderAmount,
                FinalAmount = validation.FinalAmount
            });
        }

        // POST: api/Coupons (Admin only)
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<ActionResult<Coupon>> CreateCoupon(CouponCreateDto couponDto)
        {
            // Check if coupon code already exists
            if (await _context.Coupons.AnyAsync(c => c.CouponCode == couponDto.CouponCode.ToUpper()))
            {
                return BadRequest("Bu kupon kodu zaten mevcut");
            }

            var coupon = new Coupon
            {
                CouponCode = couponDto.CouponCode.ToUpper(),
                CouponType = couponDto.CouponType, // 0 = Percentage, 1 = Fixed
                DiscountValue = couponDto.DiscountValue,
                MinOrderAmount = couponDto.MinOrderAmount,
                MaxDiscount = couponDto.MaxDiscount,
                ValidFrom = couponDto.ValidFrom,
                ValidUntil = couponDto.ValidUntil,
                UsageLimit = couponDto.UsageLimit,
                UsedCount = 0,
                CouponStatus = 1, // 1 = Active
                CreatedAt = DateTime.Now
            };

            _context.Coupons.Add(coupon);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCoupon), new { id = coupon.CouponId }, coupon);
        }

        // PUT: api/Coupons/5 (Admin only)
        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCoupon(int id, CouponUpdateDto couponDto)
        {
            var coupon = await _context.Coupons.FindAsync(id);

            if (coupon == null)
            {
                return NotFound();
            }

            // Check if new coupon code conflicts with existing ones
            if (couponDto.CouponCode.ToUpper() != coupon.CouponCode)
            {
                if (await _context.Coupons.AnyAsync(c => c.CouponCode == couponDto.CouponCode.ToUpper() && c.CouponId != id))
                {
                    return BadRequest("Bu kupon kodu zaten mevcut");
                }
                coupon.CouponCode = couponDto.CouponCode.ToUpper();
            }

            coupon.CouponType = couponDto.CouponType;
            coupon.DiscountValue = couponDto.DiscountValue;
            coupon.MinOrderAmount = couponDto.MinOrderAmount;
            coupon.MaxDiscount = couponDto.MaxDiscount;
            coupon.ValidFrom = couponDto.ValidFrom;
            coupon.ValidUntil = couponDto.ValidUntil;
            coupon.UsageLimit = couponDto.UsageLimit;
            coupon.CouponStatus = couponDto.CouponStatus;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CouponExists(id))
                {
                    return NotFound();
                }
                throw;
            }

            return NoContent();
        }

        // DELETE: api/Coupons/5 (Admin only)
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCoupon(int id)
        {
            var coupon = await _context.Coupons.FindAsync(id);
            if (coupon == null)
            {
                return NotFound();
            }

            // Check if coupon is used in any orders
            var isUsed = await _context.Orders.AnyAsync(o => o.CouponId == id);
            if (isUsed)
            {
                return BadRequest("Bu kupon siparişlerde kullanıldığı için silinemiyor. Pasif hale getirebilirsiniz.");
            }

            _context.Coupons.Remove(coupon);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // PATCH: api/Coupons/5/toggle (Admin only)
        [Authorize(Roles = "Admin")]
        [HttpPatch("{id}/toggle")]
        public async Task<IActionResult> ToggleCouponStatus(int id)
        {
            var coupon = await _context.Coupons.FindAsync(id);
            if (coupon == null)
            {
                return NotFound();
            }

            coupon.CouponStatus = (byte)(coupon.CouponStatus == 1 ? 0 : 1);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                isActive = coupon.CouponStatus == 1,
                message = $"Kupon {(coupon.CouponStatus == 1 ? "aktif" : "pasif")} edildi"
            });
        }

        // GET: api/Coupons/active (Admin only)
        [Authorize(Roles = "Admin")]
        [HttpGet("active")]
        public async Task<ActionResult<IEnumerable<object>>> GetActiveCoupons()
        {
            return await _context.Coupons
                .Where(c => c.CouponStatus == 1 &&
                           c.ValidFrom <= DateTime.Now &&
                           (!c.ValidUntil.HasValue || c.ValidUntil.Value >= DateTime.Now))
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new
                {
                    c.CouponId,
                    c.CouponCode,
                    CouponType = c.CouponType == 0 ? "Percentage" : "Fixed",
                    c.DiscountValue,
                    c.MinOrderAmount,
                    c.MaxDiscount,
                    c.UsageLimit,
                    c.UsedCount,
                    c.ValidFrom,
                    c.ValidUntil
                })
                .ToListAsync();
        }

        // GET: api/Coupons/stats (Admin only)
        [Authorize(Roles = "Admin")]
        [HttpGet("stats")]
        public async Task<ActionResult<object>> GetCouponStats()
        {
            var totalCoupons = await _context.Coupons.CountAsync();
            var activeCoupons = await _context.Coupons.CountAsync(c => c.CouponStatus == 1);
            var expiredCoupons = await _context.Coupons.CountAsync(c => c.ValidUntil.HasValue && c.ValidUntil.Value < DateTime.Now);
            var usedCoupons = await _context.Coupons.CountAsync(c => c.UsedCount > 0);

            var topCoupons = await _context.Coupons
                .Where(c => c.UsedCount > 0)
                .OrderByDescending(c => c.UsedCount)
                .Take(5)
                .Select(c => new
                {
                    c.CouponCode,
                    c.UsedCount,
                    CouponType = c.CouponType == 0 ? "Percentage" : "Fixed",
                    c.DiscountValue
                })
                .ToListAsync();

            return Ok(new
            {
                totalCoupons,
                activeCoupons,
                expiredCoupons,
                usedCoupons,
                topCoupons
            });
        }

        private decimal CalculateDiscount(Coupon coupon, decimal orderAmount)
        {
            if (coupon.CouponType == 0) // Percentage
            {
                return (orderAmount * coupon.DiscountValue) / 100;
            }
            else // Fixed amount (CouponType == 1)
            {
                return coupon.DiscountValue;
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

        private bool CouponExists(int id)
        {
            return _context.Coupons.Any(e => e.CouponId == id);
        }
    }

}