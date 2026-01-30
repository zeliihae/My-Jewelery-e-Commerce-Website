using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JeweleryStore1.Data;
using JeweleryStore1.Models;
using System.Security.Claims;

namespace JeweleryStore1.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("api/[controller]")]
    [ApiController]
    public class OrderItemsController : ControllerBase
    {
        private readonly JewDbContext _context;
        
        public OrderItemsController(JewDbContext context)
        {
            _context = context;
        }

        // GET: api/OrderItems/order/5
        [HttpGet("order/{orderId}")]
        public async Task<ActionResult<IEnumerable<OrderItem>>> GetOrderItems(int orderId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            // Siparişin kullanıcıya ait olup olmadığını kontrol et
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
            {
                return NotFound("Sipariş bulunamadı.");
            }

            // Admin değilse sadece kendi siparişlerini görebilir
            if (!User.IsInRole("Admin") && order.UserId != userId)
            {
                return Forbid();
            }

            var orderItems = await _context.OrderItems
                .Where(oi => oi.OrderId == orderId)
                .Include(oi => oi.Product)
                    .ThenInclude(p => p.ProductImages)
                .ToListAsync();

            return Ok(orderItems);
        }

        // GET: api/OrderItems/5
        [HttpGet("{id}")]
        public async Task<ActionResult<OrderItem>> GetOrderItem(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            var orderItem = await _context.OrderItems
                .Include(oi => oi.Product)
                .Include(oi => oi.Order)
                .FirstOrDefaultAsync(oi => oi.OrderItemId == id);

            if (orderItem == null)
            {
                return NotFound();
            }

            // Admin değilse sadece kendi sipariş ürünlerini görebilir
            if (!User.IsInRole("Admin") && orderItem.Order.UserId != userId)
            {
                return Forbid();
            }

            return Ok(orderItem);
        }

        // POST: api/OrderItems
        [HttpPost]
        public async Task<ActionResult<OrderItem>> CreateOrderItem(OrderItem orderItem)
        {
            // Sadece admin sipariş ürünü ekleyebilir
            if (!User.IsInRole("Admin"))
            {
                return Forbid();
            }

            // Siparişin var olup olmadığını kontrol et
            var orderExists = await _context.Orders.AnyAsync(o => o.OrderId == orderItem.OrderId);
            if (!orderExists)
            {
                return BadRequest("Sipariş bulunamadı.");
            }

            // Ürünün var olup olmadığını ve stokta olup olmadığını kontrol et
            var product = await _context.Products.FindAsync(orderItem.ProductId);
            if (product == null)
            {
                return BadRequest("Ürün bulunamadı.");
            }

            if (product.ProductStock < orderItem.Quantity)
            {
                return BadRequest("Yetersiz stok.");
            }

            // Subtotal hesapla
            var price = orderItem.DiscountPrice ?? orderItem.UnitPrice;
            orderItem.Subtotal = price * orderItem.Quantity;

            _context.OrderItems.Add(orderItem);

            // Stok güncelle
            product.ProductStock -= orderItem.Quantity;

            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetOrderItem), new { id = orderItem.OrderItemId }, orderItem);
        }

        // PUT: api/OrderItems/5
        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateOrderItem(int id, OrderItem orderItem)
        {
            if (id != orderItem.OrderItemId)
            {
                return BadRequest("ID eşleşmiyor.");
            }

            var existingItem = await _context.OrderItems
                .Include(oi => oi.Product)
                .FirstOrDefaultAsync(oi => oi.OrderItemId == id);

            if (existingItem == null)
            {
                return NotFound();
            }

            // Miktar değişmişse stok kontrolü yap
            if (existingItem.Quantity != orderItem.Quantity)
            {
                var quantityDiff = orderItem.Quantity - existingItem.Quantity;
                var product = existingItem.Product;

                if (quantityDiff > 0 && product.ProductStock < quantityDiff)
                {
                    return BadRequest("Yetersiz stok.");
                }

                // Stok güncelle
                product.ProductStock -= quantityDiff;
            }

            // Subtotal güncelle
            var price = orderItem.DiscountPrice ?? orderItem.UnitPrice;
            orderItem.Subtotal = price * orderItem.Quantity;

            existingItem.Quantity = orderItem.Quantity;
            existingItem.UnitPrice = orderItem.UnitPrice;
            existingItem.DiscountPrice = orderItem.DiscountPrice;
            existingItem.Subtotal = orderItem.Subtotal;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!OrderItemExists(id))
                {
                    return NotFound();
                }
                throw;
            }

            return NoContent();
        }

        // DELETE: api/OrderItems/5
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrderItem(int id)
        {
            var orderItem = await _context.OrderItems
                .Include(oi => oi.Product)
                .FirstOrDefaultAsync(oi => oi.OrderItemId == id);

            if (orderItem == null)
            {
                return NotFound();
            }

            // Stoku geri ekle
            orderItem.Product.ProductStock += orderItem.Quantity;

            _context.OrderItems.Remove(orderItem);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // GET: api/OrderItems/product/5/orders
        [Authorize(Roles = "Admin")]
        [HttpGet("product/{productId}/orders")]
        public async Task<ActionResult<IEnumerable<object>>> GetProductOrderHistory(int productId)
        {
            var orderItems = await _context.OrderItems
                .Where(oi => oi.ProductId == productId)
                .Include(oi => oi.Order)
                    .ThenInclude(o => o.User)
                .OrderByDescending(oi => oi.Order.OrderDate)
                .Select(oi => new
                {
                    oi.OrderItemId,
                    oi.OrderId,
                    TrackingNumber = oi.Order.TrackingNumber,
                    oi.Quantity,
                    oi.UnitPrice,
                    oi.DiscountPrice,
                    oi.Subtotal,
                    OrderDate = oi.Order.OrderDate,
                    OrderStatus = oi.Order.OrderStatus,
                    CustomerName = oi.Order.User.UserName,
                    CustomerEmail = oi.Order.User.UserEmail
                })
                .ToListAsync();

            return Ok(orderItems);
        }

        private bool OrderItemExists(int id)
        {
            return _context.OrderItems.Any(e => e.OrderItemId == id);
        }
    }
}