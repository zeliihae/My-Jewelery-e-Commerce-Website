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



namespace JeweleryStore1.Controllers.Admin
{
   
    [Route("api/admin/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly JewDbContext _context;

        public OrdersController(JewDbContext context)
        {
            _context = context;
        }

        // GET: api/admin/Orders
        [HttpGet]
        public async Task<ActionResult<object>> GetOrders(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? search = null,
            [FromQuery] string? status = null, // Query'den hala string gelebilir (örn: "0" veya "pending")
            [FromQuery] byte? paymentStatus = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
          
            var query = _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.Coupon)
                .AsQueryable();

            
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(o =>
                    o.TrackingNumber.Contains(search) ||
                    o.User.UserName.Contains(search) ||
                    o.User.UserEmail.Contains(search));
            }

            if (!string.IsNullOrEmpty(status))
            {
                byte statusValue = ConvertStatusToByte(status);
                query = query.Where(o => o.OrderStatus == statusValue);
            }

            // Filter by payment status
            if (paymentStatus.HasValue)
            {
                query = query.Where(o => o.PaymentStatus == paymentStatus.Value);
            }

            // Filter by date range
            if (startDate.HasValue)
            {
                query = query.Where(o => o.OrderDate >= startDate.Value);
            }
            if (endDate.HasValue)
            {
                query = query.Where(o => o.OrderDate <= endDate.Value.AddDays(1));
            }

            var totalCount = await query.CountAsync();

            var orders = await query
                .OrderByDescending(o => o.OrderDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(o => new
                {
                    o.OrderId,
                    o.TrackingNumber,
                    o.UserId,
                    UserName = o.User.UserName,
                    UserEmail = o.User.UserEmail,
                    o.OrderDate,
                
                    OrderStatus = o.OrderStatus.ToString(),
                    o.TotalAmount,
                    o.DiscountAmount,
                    FinalAmount = o.TotalAmount - o.DiscountAmount,
                    o.PaymentMethod,
                    o.PaymentStatus,
                    PaymentStatusText = o.PaymentStatus == 1 ? "Paid" : "Pending",
                    ItemCount = o.OrderItems.Count,
                    ItemQuantity = o.OrderItems.Sum(oi => oi.Quantity),
                    CouponCode = o.Coupon != null ? o.Coupon.CouponCode : null,
                    o.CreatedAt
                })
                .ToListAsync();

            return Ok(new
            {
                orders,
                totalCount,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            });
        }

        
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetOrder(int id)
        {
            

            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                        .ThenInclude(p => p.ProductImages)
                .Include(o => o.Coupon)
                .Include(o => o.OrderStatusHistories)
                .Where(o => o.OrderId == id)
                .Select(o => new
                {
                    o.OrderId,
                    o.TrackingNumber,
                    o.UserId,
                    User = new
                    {
                        o.User.UserId,
                        o.User.UserName,
                        o.User.UserEmail,
                        o.User.UserPhone
                    },
                    o.OrderDate,
                    // ✅ DÜZELTİLDİ: byte to string
                    OrderStatus = o.OrderStatus.ToString(),
                    o.TotalAmount,
                    o.DiscountAmount,
                    FinalAmount = o.TotalAmount - o.DiscountAmount,
                    o.ShippingAddressId,
                    o.BillingAddressId,
                    o.PaymentMethod,
                    o.PaymentStatus,
                    PaymentStatusText = o.PaymentStatus == 1 ? "Paid" : "Pending",
                    o.Notes,
                    o.CreatedAt,
                    Coupon = o.Coupon != null ? new
                    {
                        o.Coupon.CouponId,
                        o.Coupon.CouponCode,
                        CouponType = o.Coupon.CouponType == 0 ? "Percentage" : "Fixed",
                        o.Coupon.DiscountValue
                    } : null,
                    OrderItems = o.OrderItems.Select(oi => new
                    {
                        oi.OrderItemId,
                        oi.ProductId,
                        ProductName = oi.Product.ProductName,
                        ProductImage = oi.Product.ProductImages
                            .Where(pi => pi.IsPrimary)
                            .Select(pi => pi.ImageUrl)
                            .FirstOrDefault()
                            ?? oi.Product.ProductImages
                                .OrderBy(pi => pi.ImageOrder)
                                .Select(pi => pi.ImageUrl)
                                .FirstOrDefault()
                            ?? oi.Product.ProductImage,
                        oi.Quantity,
                        oi.UnitPrice,
                        oi.DiscountPrice,
                        FinalPrice = oi.DiscountPrice ?? oi.UnitPrice,
                        oi.Subtotal
                    }),
                    StatusHistory = o.OrderStatusHistories
                        .OrderByDescending(h => h.ChangedAt)
                        .Select(h => new
                        {
                            h.HistoryId,
                            h.OldStatus,
                            h.NewStatus,
                            h.ChangedBy,
                            h.Notes,
                            h.ChangedAt
                        })
                })
                .FirstOrDefaultAsync();

            if (order == null)
            {
                return NotFound();
            }

            return order;
        }

        
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] OrderStatusUpdateDto statusDto)
        {
         

            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            var adminUserId = GetCurrentUserId();
            // ✅ DÜZELTİLDİ: OrderStatus zaten byte
            var oldStatus = order.OrderStatus;

            // ✅ DÜZELTİLDİ: string gelen yeni status'u byte'a çeviriyoruz
            byte newStatusByte = ConvertStatusToByte(statusDto.NewStatus);
            order.OrderStatus = newStatusByte;

            // Create status history record
            var statusHistory = new OrderStatusHistory
            {
                OrderId = id,
                OldStatus = oldStatus, // Zaten byte
                NewStatus = newStatusByte,
                ChangedBy = adminUserId,
                Notes = statusDto.Notes,
                ChangedAt = DateTime.Now
            };

            _context.OrderStatusHistories.Add(statusHistory);

            // ✅ DÜZELTİLDİ: İptal kontrolü (eğer string geliyorsa "4" iptal kodunuzdur)
            if (newStatusByte == 4 || statusDto.NewStatus.ToLower() == "cancelled")
            {
                var orderItems = await _context.OrderItems
                    .Where(oi => oi.OrderId == id)
                    .ToListAsync();

                foreach (var item in orderItems)
                {
                    var product = await _context.Products.FindAsync(item.ProductId);
                    if (product != null)
                    {
                        product.ProductStock += item.Quantity;
                    }
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                orderId = order.OrderId,
                oldStatus = oldStatus.ToString(),
                newStatus = order.OrderStatus.ToString(),
                message = "Sipariş durumu güncellendi"
            });
        }

        

       

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out int userId) ? userId : 0;
        }

      
        private byte ConvertStatusToByte(string status)
        {
            if (byte.TryParse(status, out byte result)) return result;

            return status.ToLower() switch
            {
                "pending" => 0,
                "processing" => 1,
                "shipped" => 2,
                "delivered" => 3,
                "cancelled" => 4,
                _ => 0
            };
        }

     
    }
}