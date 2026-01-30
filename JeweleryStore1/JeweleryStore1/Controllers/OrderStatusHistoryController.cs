using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JeweleryStore1.Data;
using JeweleryStore1.Models;
using System.Security.Claims;

namespace JeweleryStore1.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class OrderStatusHistoryController : ControllerBase
    {
        private readonly JewDbContext _context;

        public OrderStatusHistoryController(JewDbContext context)
        {
            _context = context;
        }

        [HttpGet("order/{orderId}")]
        public async Task<ActionResult<IEnumerable<object>>> GetOrderStatusHistory(int orderId)
        {
            var userId = GetCurrentUserId();

            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return NotFound("Sipariş bulunamadı.");

            if (!User.IsInRole("Admin") && order.UserId != userId) return Forbid();

            var history = await _context.OrderStatusHistories
                .Where(h => h.OrderId == orderId)
                .OrderByDescending(h => h.ChangedAt)
                .Select(h => new
                {
                    h.HistoryId,
                    h.OrderId,
                    h.OldStatus,
                    OldStatusText = GetStatusText(h.OldStatus),
                    h.NewStatus,
                    NewStatusText = GetStatusText(h.NewStatus),
                    h.ChangedBy,
                    // Değişiklik: u.UserId karşılaştırmasını int cast yaparak garantiliyoruz
                    ChangedByName = h.ChangedBy != null
                        ? _context.Users.Where(u => u.UserId == (int)h.ChangedBy).Select(u => u.UserName).FirstOrDefault()
                        : "System",
                    h.Notes,
                    h.ChangedAt
                })
                .ToListAsync();

            return Ok(history);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("order/{orderId}/status")]
        public async Task<ActionResult> UpdateOrderStatus(int orderId, [FromBody] UpdateStatusRequest request)
        {
            var adminId = GetCurrentUserId();

            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return NotFound("Sipariş bulunamadı.");

            // Düzeltme: string'den byte'a güvenli dönüşüm (CS0029 çözümü)
            byte oldStatusByte = order.OrderStatus;

            var history = new OrderStatusHistory
            {
                OrderId = orderId,
                OldStatus = oldStatusByte,
                NewStatus = (byte)request.NewStatus, // Açıkça byte cast yapıldı
                ChangedBy = adminId,
                Notes = request.Notes,
                ChangedAt = DateTime.Now
            };

            _context.OrderStatusHistories.Add(history);

            // Veritabanına string olarak kaydetme
            order.OrderStatus = request.NewStatus;

            if (request.NewStatus == 4) // 4 = İptal Edildi
            {
                var orderItems = await _context.OrderItems
                    .Where(oi => oi.OrderId == orderId)
                    .Include(oi => oi.Product)
                    .ToListAsync();

                foreach (var item in orderItems)
                {
                    if (item.Product != null)
                    {
                        item.Product.ProductStock += item.Quantity;
                    }
                }
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Sipariş durumu güncellendi.", newStatus = GetStatusText(request.NewStatus) });
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("recent")]
        public async Task<ActionResult<IEnumerable<object>>> GetRecentStatusChanges(int limit = 50)
        {
            var recentChanges = await _context.OrderStatusHistories
                .Include(h => h.Order)
                    .ThenInclude(o => o.User)
                .OrderByDescending(h => h.ChangedAt)
                .Take(limit)
                .Select(h => new
                {
                    h.HistoryId,
                    h.OrderId,
                    TrackingNumber = h.Order.TrackingNumber,
                    CustomerName = h.Order.User.UserName ?? "Bilinmeyen",
                    h.OldStatus,
                    OldStatusText = GetStatusText(h.OldStatus),
                    h.NewStatus,
                    NewStatusText = GetStatusText(h.NewStatus),
                    // h.ChangedBy cast işlemi buraya da eklendi
                    ChangedByName = h.ChangedBy != null
                        ? _context.Users.Where(u => u.UserId == (int)h.ChangedBy).Select(u => u.UserName).FirstOrDefault()
                        : "System",
                    h.Notes,
                    h.ChangedAt
                })
                .ToListAsync();

            return Ok(recentChanges);
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out int userId) ? userId : 0;
        }

        private static string GetStatusText(byte? status)
        {
            return status switch
            {
                0 => "Hazırlanıyor",
                1 => "Onaylandı",
                2 => "Kargoya Verildi",
                3 => "Teslim Edildi",
                4 => "İptal Edildi",
                _ => "Bilinmeyen"
            };
        }
    }

    public class UpdateStatusRequest
    {
        public byte NewStatus { get; set; }
        public string? Notes { get; set; }
    }
}