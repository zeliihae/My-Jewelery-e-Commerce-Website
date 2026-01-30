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
    public class ReceiptController : ControllerBase
    {
        private readonly JewDbContext _context;

        public ReceiptController(JewDbContext context)
        {
            _context = context;
        }

        // ========================================
        // POST: api/Receipt - FATURA OLUŞTUR
        // ========================================
        [HttpPost]
        public async Task<ActionResult<Receipt>> CreateReceipt([FromBody] CreateReceiptRequest request)
        {
            try
            {
                Console.WriteLine($"📥 Fatura isteği alındı - OrderId: {request.OrderId}");

                // 1. Sipariş kontrolü
                var order = await _context.Orders
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product)
                    .Include(o => o.User)
                    .Include(o => o.Coupon) // ✅ Kupon bilgisini de çek
                    .FirstOrDefaultAsync(o => o.OrderId == request.OrderId);

                if (order == null)
                {
                    Console.WriteLine($"❌ Sipariş bulunamadı: {request.OrderId}");
                    return BadRequest("Sipariş bulunamadı.");
                }

                Console.WriteLine($"✅ Sipariş bulundu: {order.TrackingNumber}");
                Console.WriteLine($"💰 Sipariş - Total: {order.TotalAmount}, Discount: {order.DiscountAmount}");

                // 2. Duplicate kontrol
                var existingReceipt = await _context.Receipts
                    .FirstOrDefaultAsync(r => r.OrderId == request.OrderId);

                if (existingReceipt != null)
                {
                    Console.WriteLine($"⚠️ Fatura zaten var: {existingReceipt.ReceiptNumber}");
                    return BadRequest("Bu sipariş için zaten bir fatura mevcut.");
                }

                // ========================================
                // 3. BASIT HESAPLAMA - Backend'deki değerleri kullan
                // ========================================
                decimal subtotal = order.TotalAmount;           // Sepetteki ara toplam (KDV dahil)
                decimal discountAmount = order.DiscountAmount;  // Backend'in hesapladığı indirim
                decimal finalTotal = subtotal - discountAmount; // İndirimli toplam (KDV dahil)

                // KDV sadece gösterim için hesapla (zaten toplam içinde)
                decimal taxRate = 0.18m;
                decimal taxAmount = finalTotal / (1 + taxRate) * taxRate;

                Console.WriteLine($"📊 Fatura Değerleri:");
                Console.WriteLine($"   Ara Toplam: {subtotal}");
                Console.WriteLine($"   İndirim: {discountAmount}");
                Console.WriteLine($"   Final Toplam: {finalTotal}");
                Console.WriteLine($"   KDV (gösterim): {taxAmount}");

                // 4. Fatura oluştur
                var receipt = new Receipt
                {
                    OrderId = request.OrderId,
                    ReceiptNumber = GenerateReceiptNumber(),
                    ReceiptDate = DateTime.Now,
                    TotalAmount = finalTotal,        // İndirimli toplam
                    TaxAmount = taxAmount,           // KDV (gösterim)
                    ReceiptType = 1,
                    ReceiptStatus = 1,
                    Notes = request.Description ?? "Online Order"
                };

                _context.Receipts.Add(receipt);
                await _context.SaveChangesAsync();

                Console.WriteLine($"✅ Fatura kaydedildi: {receipt.ReceiptNumber} (ID: {receipt.ReceiptId})");

                // 5. Include'lu veriyi çek ve döndür
                var createdReceipt = await _context.Receipts
                    .Include(r => r.Order)
                        .ThenInclude(o => o.User)
                    .Include(r => r.Order.OrderItems)
                        .ThenInclude(oi => oi.Product)
                    .Include(r => r.Order.Coupon) // ✅ Kupon bilgisini de dahil et
                    .FirstOrDefaultAsync(r => r.ReceiptId == receipt.ReceiptId);

                if (createdReceipt == null)
                {
                    Console.WriteLine("❌ Fatura kaydedildi ama tekrar bulunamadı!");
                    return StatusCode(500, "Fatura oluşturuldu ancak alınamadı.");
                }

                Console.WriteLine($"✅ Fatura frontend'e gönderiliyor: {createdReceipt.ReceiptNumber}");
                return Ok(createdReceipt);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ HATA: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                return StatusCode(500, $"Bir hata oluştu: {ex.Message}");
            }
        }

        // ========================================
        // GET: api/Receipt/order/5
        // ========================================
        [HttpGet("order/{orderId}")]
        public async Task<ActionResult<Receipt>> GetReceiptByOrderId(int orderId)
        {
            try
            {
                Console.WriteLine($"📥 Fatura çekiliyor - OrderId: {orderId}");

                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                var order = await _context.Orders.FindAsync(orderId);
                if (order == null)
                {
                    Console.WriteLine($"❌ Sipariş bulunamadı: {orderId}");
                    return NotFound("Sipariş bulunamadı.");
                }

                // Yetki kontrolü
                if (!User.IsInRole("Admin") && order.UserId != userId)
                {
                    Console.WriteLine($"❌ Yetkisiz erişim denemesi - UserId: {userId}");
                    return Forbid();
                }

                var receipt = await _context.Receipts
                    .Include(r => r.Order)
                        .ThenInclude(o => o.User)
                    .Include(r => r.Order.OrderItems)
                        .ThenInclude(oi => oi.Product)
                    .Include(r => r.Order.Coupon) // ✅ Kupon bilgisi
                    .FirstOrDefaultAsync(r => r.OrderId == orderId);

                if (receipt == null)
                {
                    Console.WriteLine($"❌ Fatura bulunamadı - OrderId: {orderId}");
                    return NotFound("Bu sipariş için fatura bulunamadı.");
                }

                Console.WriteLine($"✅ Fatura bulundu: {receipt.ReceiptNumber}");
                return Ok(receipt);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ HATA: {ex.Message}");
                return StatusCode(500, $"Bir hata oluştu: {ex.Message}");
            }
        }

        // ========================================
        // GET: api/Receipt
        // ========================================
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Receipt>>> GetReceipts()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            if (User.IsInRole("Admin"))
            {
                var allReceipts = await _context.Receipts
                    .Include(r => r.Order)
                        .ThenInclude(o => o.User)
                    .OrderByDescending(r => r.ReceiptDate)
                    .ToListAsync();

                return Ok(allReceipts);
            }
            else
            {
                var userReceipts = await _context.Receipts
                    .Include(r => r.Order)
                    .Where(r => r.Order.UserId == userId)
                    .OrderByDescending(r => r.ReceiptDate)
                    .ToListAsync();

                return Ok(userReceipts);
            }
        }

        // ========================================
        // GET: api/Receipt/5
        // ========================================
        [HttpGet("{id}")]
        public async Task<ActionResult<Receipt>> GetReceipt(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            var receipt = await _context.Receipts
                .Include(r => r.Order)
                    .ThenInclude(o => o.User)
                .Include(r => r.Order.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(r => r.ReceiptId == id);

            if (receipt == null)
            {
                return NotFound("Fatura bulunamadı.");
            }

            if (!User.IsInRole("Admin") && receipt.Order.UserId != userId)
            {
                return Forbid();
            }

            return Ok(receipt);
        }

        // ========================================
        // DELETE: api/Receipt/5
        // ========================================
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteReceipt(int id)
        {
            var receipt = await _context.Receipts.FindAsync(id);
            if (receipt == null)
            {
                return NotFound();
            }

            _context.Receipts.Remove(receipt);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // ========================================
        // HELPER METHOD
        // ========================================
        private string GenerateReceiptNumber()
        {
            var date = DateTime.Now.ToString("yyyyMMdd");
            var random = new Random().Next(10000, 99999);
            return $"RCP-{date}-{random}";
        }
    }

    // ========================================
    // REQUEST MODELS
    // ========================================
    public class CreateReceiptRequest
    {
        public int OrderId { get; set; }
        public string? Description { get; set; }
    }
}