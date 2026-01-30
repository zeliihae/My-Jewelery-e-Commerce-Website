using JeweleryStore1.Data;
using JeweleryStore1.DTOs;
using JeweleryStore1.Exceptions;
using JeweleryStore1.Models;
using JeweleryStore1.Models.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace JeweleryStore1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]

    public class OrdersController : BaseApiController
    {
        private readonly JewDbContext _context;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(JewDbContext context, ILogger<OrdersController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpPost]
        public async Task<ActionResult> CreateOrder([FromBody] CreateOrderDto createOrderDto)
        {
            var userId = GetCurrentUserId();

            // 1. Sepet Kontrolü
            var cart = await _context.Carts
                .Include(c => c.CartItems).ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null || !cart.CartItems.Any())
                return BadRequest(new { message = "Sepetiniz boş" });

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 2. Ara Toplam Hesapla
                decimal orderTotal = cart.CartItems.Sum(ci =>
                    (ci.Product.ProductDiscountPrice ?? ci.Product.ProductPrice) * ci.Quantity);

                // 3. KUPON KONTROLÜ VE İNDİRİM HESAPLAMA
                decimal discountAmount = 0;
                int? couponId = null;

                if (createOrderDto.CouponId.HasValue)
                {
                    var coupon = await _context.Coupons.FindAsync(createOrderDto.CouponId.Value);

                    // Property isimleri modelinize (ValidUntil, CouponStatus vb.) göre düzeltildi
                    if (coupon != null &&
                        coupon.CouponStatus == 1 && // 1 = Active varsayıldı
                        coupon.ValidFrom <= DateTime.Now &&
                        (!coupon.ValidUntil.HasValue || coupon.ValidUntil >= DateTime.Now) &&
                        (!coupon.UsageLimit.HasValue || coupon.UsedCount < coupon.UsageLimit))
                    {
                        if (orderTotal >= coupon.MinOrderAmount)
                        {
                            // indirim hesapla
                            if (coupon.CouponType == 0) // Percentage
                            {
                                discountAmount = orderTotal * (coupon.DiscountValue / 100m);
                                if (coupon.MaxDiscount.HasValue && discountAmount > coupon.MaxDiscount.Value)
                                    discountAmount = coupon.MaxDiscount.Value;
                            }
                            else if (coupon.CouponType == 1) // Fixed Amount
                            {
                                discountAmount = coupon.DiscountValue;
                            }

                            if (discountAmount > orderTotal) discountAmount = orderTotal;

                            couponId = coupon.CouponId;
                            coupon.UsedCount++; // Kupon kullanım sayısını artır
                            _context.Entry(coupon).State = EntityState.Modified;
                        }
                    }
                }

                // 4. Sipariş Oluşturma
                var order = new Order
                {
                    UserId = userId,
                    TotalAmount = orderTotal,
                    DiscountAmount = discountAmount,
                    CouponId = couponId,
                    OrderStatus = 0, // Pending
                    OrderDate = DateTime.Now,
                    CreatedAt = DateTime.Now,
                    ShippingAddressId = createOrderDto.ShippingAddressId,
                    BillingAddressId = createOrderDto.BillingAddressId,
                    PaymentMethod = createOrderDto.PaymentMethod ?? "Credit Card",
                    TrackingNumber = GenerateTrackingNumber()
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // 5. Kalemleri Ekle ve Stok Düş
                foreach (var cartItem in cart.CartItems)
                {
                    var unitPrice = cartItem.Product.ProductDiscountPrice ?? cartItem.Product.ProductPrice;
                    var orderItem = new OrderItem
                    {
                        OrderId = order.OrderId,
                        ProductId = cartItem.ProductId,
                        Quantity = cartItem.Quantity,
                        UnitPrice = unitPrice,
                        Subtotal = unitPrice * cartItem.Quantity
                    };
                    _context.OrderItems.Add(orderItem);
                    cartItem.Product.ProductStock -= cartItem.Quantity;
                }

                // 6. Sepeti Temizle
                _context.CartItems.RemoveRange(cart.CartItems);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new
                {
                    success = true,
                    orderId = order.OrderId,
                    message = "Sipariş başarıyla oluşturuldu"
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { message = "Hata oluştu", error = ex.Message });
            }
        }

        [HttpGet("my")]
        public async Task<ActionResult<ApiResponse<IEnumerable<OrderResponseDto>>>> GetMyOrders()
        {
            var userId = GetCurrentUserId();
            _logger.LogInformation("GetMyOrders tetiklendi. UserId: {UserId}", userId);

            var orders = await _context.Orders
                .AsNoTracking()
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.Coupon) // ✅ Kupon bilgisi
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            if (orders == null || !orders.Any())
            {
                return Ok(ApiResponse<IEnumerable<OrderResponseDto>>.SuccessResponse(
                    new List<OrderResponseDto>(), "Henüz bir siparişiniz bulunmamaktadır."));
            }

            var orderResponses = orders.Select(order => MapToOrderResponse(order)).ToList();

            return Ok(ApiResponse<IEnumerable<OrderResponseDto>>.SuccessResponse(
                orderResponses, $"{orderResponses.Count} adet sipariş bulundu."));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<OrderResponseDto>>> GetOrder(int id)
        {
            var userId = GetCurrentUserId();
            var isAdmin = IsAdmin();

            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.Coupon) // ✅ Kupon bilgisi
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null) throw new NotFoundException("Sipariş", id);

            if (!isAdmin && order.UserId != userId)
                throw new ForbiddenException("Bu siparişi görüntüleme yetkiniz yok");

            return Ok(ApiResponse<OrderResponseDto>.SuccessResponse(MapToOrderResponse(order), "Sipariş detayları getirildi"));
        }


        private OrderResponseDto MapToOrderResponse(Order order)
        {
            return new OrderResponseDto
            {
                OrderId = order.OrderId,
                UserId = order.UserId,
                UserName = order.User?.UserName ?? "Bilinmeyen Kullanıcı",
                OrderTotal = order.TotalAmount,
                OrderStatus = GetStatusName(order.OrderStatus),
                OrderCreatedAt = order.CreatedAt,
                PaymentMethod = order.PaymentMethod ?? "Belirtilmedi",
                TrackingNumber = order.TrackingNumber ?? "-",
                Items = order.OrderItems?.Select(oi => new OrderItemResponseDto
                {
                    OrderItemId = oi.OrderItemId,
                    ProductId = oi.ProductId,
                    ProductName = oi.Product?.ProductName ?? "Ürün Silinmiş",
                    ProductImage = oi.Product?.ProductImage,
                    Quantity = oi.Quantity,
                    Price = oi.UnitPrice,
                    Discount = oi.DiscountPrice,
                    Subtotal = oi.Subtotal
                }).ToList() ?? new List<OrderItemResponseDto>()
            };
        }

        [HttpGet("admin/all")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<IEnumerable<OrderResponseDto>>>> GetAllOrders()
        {
            var orders = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.Coupon) // ✅ Kupon bilgisi
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            var orderResponses = orders.Select(order => MapToOrderResponse(order)).ToList();
            return Ok(ApiResponse<IEnumerable<OrderResponseDto>>.SuccessResponse(orderResponses, "Tüm siparişler getirildi."));
        }

        private string GenerateTrackingNumber() => $"JW{DateTime.Now:yyyyMMddHHmmss}{new Random().Next(1000, 9999)}";

        private string GetStatusName(byte status)
        {
            return status switch
            {
                0 => "Pending",
                1 => "Processing",
                2 => "Shipped",
                3 => "Delivered",
                4 => "Cancelled",
                _ => "Unknown"
            };
        }
    }
}